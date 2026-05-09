using System.Globalization;
using System.Text.RegularExpressions;
using EMS.Application.DTOs.Employee;
using Pukar.Shared;
using EMS.Application.Mapping;
using EMS.Application.Services.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using JobPositionEntity = EMS.Domain.DbModels.JobPosition;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeService : IEmployeeService
{
    private static readonly Regex EmployeeNumberSequence = new(
        @"^EMP(\d+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private readonly IBaseRepository<Employee> _repository;
    private readonly IBaseRepository<JobPositionEntity> _jobPositionRepository;
    private readonly IBaseRepository<EmployeePositionHistory> _positionHistoryRepository;
    private readonly IBaseRepository<EmployeeDepartmentHistory> _departmentHistoryRepository;
    private readonly IBaseRepository<EmployeeManagerHistory> _managerHistoryRepository;
    private readonly IBaseRepository<EmployeeRetentionPolicy> _retentionPolicyRepository;
    private readonly IIdentityContext _identityContext;
    private readonly IEmployeeRoleSyncService _employeeRoleSyncService;

    public EmployeeService(
        IBaseRepository<Employee> repository,
        IBaseRepository<JobPositionEntity> jobPositionRepository,
        IBaseRepository<EmployeePositionHistory> positionHistoryRepository,
        IBaseRepository<EmployeeDepartmentHistory> departmentHistoryRepository,
        IBaseRepository<EmployeeManagerHistory> managerHistoryRepository,
        IBaseRepository<EmployeeRetentionPolicy> retentionPolicyRepository,
        IIdentityContext identityContext,
        IEmployeeRoleSyncService employeeRoleSyncService)
    {
        _repository = repository;
        _jobPositionRepository = jobPositionRepository;
        _positionHistoryRepository = positionHistoryRepository;
        _departmentHistoryRepository = departmentHistoryRepository;
        _managerHistoryRepository = managerHistoryRepository;
        _retentionPolicyRepository = retentionPolicyRepository;
        _identityContext = identityContext;
        _employeeRoleSyncService = employeeRoleSyncService;
    }

    public async Task<EmployeeResponseModel> CreateAsync(CreateEmployeeRequestModel request, CancellationToken cancellationToken = default)
    {
        request.Email = StringHelper.NormalizeRequired(request.Email);
        if (!StringHelper.IsValidEmail(request.Email))
            throw new BusinessRuleException("Invalid email address.");

        await EnsureJobPositionMatchesOrganizationAsync(request.OrganizationId, request.JobPositionId, cancellationToken);

        var now = DateTime.UtcNow;
        var entity = EmployeeMapper.ToEntity(request);
        entity.EmployeeNumber = await AllocateNextEmployeeNumberAsync(request.OrganizationId, cancellationToken);
        entity.CreatedAtUtc = now;
        entity.UpdatedAtUtc = now;

        await _repository.AddAsync(entity, cancellationToken);
        await AddHistoryEntriesAsync(entity, request, previousDepartmentId: null, previousJobPositionId: null, previousManagerId: null, now, cancellationToken);
        if (entity.EmploymentStatus == EmploymentStatus.Terminated)
        {
            await ApplyRetentionFromPolicyAsync(entity, now, cancellationToken);
        }
        await _repository.SaveChangesAsync(cancellationToken);
        await _employeeRoleSyncService.SyncEmployeeAsync(entity.Id, cancellationToken);

        return EmployeeMapper.ToResponse(entity);
    }

    public async Task<EmployeeResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity?.IsArchived == true)
            return null;
        return entity is null ? null : EmployeeMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<EmployeeResponseModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetQueryable().Where(e => !e.IsArchived).ToListAsync(cancellationToken);
        return list.Select(EmployeeMapper.ToResponse).ToList();
    }

    public async Task<EmployeeResponseModel?> UpdateAsync(int id, UpdateEmployeeRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        request.Email = StringHelper.NormalizeRequired(request.Email);
        if (!StringHelper.IsValidEmail(request.Email))
            throw new BusinessRuleException("Invalid email address.");

        await EnsureJobPositionMatchesOrganizationAsync(request.OrganizationId, request.JobPositionId, cancellationToken);

        var previousDepartmentId = entity.DepartmentId;
        var previousJobPositionId = entity.JobPositionId;
        var previousManagerId = entity.ManagerId;
        var previousEmploymentStatus = entity.EmploymentStatus;
        var now = DateTime.UtcNow;

        EmployeeMapper.ApplyUpdate(entity, request);
        entity.UpdatedAtUtc = now;
        await AddHistoryEntriesAsync(
            entity,
            request,
            previousDepartmentId,
            previousJobPositionId,
            previousManagerId,
            now,
            cancellationToken);

        if (previousEmploymentStatus != EmploymentStatus.Terminated &&
            entity.EmploymentStatus == EmploymentStatus.Terminated)
        {
            await ApplyRetentionFromPolicyAsync(entity, now, cancellationToken);
        }

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        if (previousJobPositionId != entity.JobPositionId)
            await _employeeRoleSyncService.SyncEmployeeAsync(entity.Id, cancellationToken);

        return EmployeeMapper.ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        var now = DateTime.UtcNow;
        entity.IsArchived = true;
        entity.ArchivedAtUtc = now;
        entity.ArchiveReason = "Archived via delete request.";
        entity.UpdatedAtUtc = now;
        if (entity.EmploymentStatus != EmploymentStatus.Terminated)
        {
            entity.EmploymentStatus = EmploymentStatus.Terminated;
            entity.IsActive = false;
        }

        await ApplyRetentionFromPolicyAsync(entity, now, cancellationToken);
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<EmployeeHistoryResponseModel?> GetHistoryAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employeeExists = await _repository.GetQueryable()
            .AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!employeeExists)
            return null;

        var positions = await _positionHistoryRepository.GetQueryable()
            .Where(h => h.EmployeeId == employeeId)
            .OrderBy(h => h.EffectiveFromUtc)
            .ThenBy(h => h.Id)
            .Select(h => new PositionHistoryItemResponseModel
            {
                Id = h.Id,
                PreviousJobPositionId = h.PreviousJobPositionId,
                NewJobPositionId = h.NewJobPositionId,
                EffectiveFromUtc = h.EffectiveFromUtc,
                EffectiveToUtc = h.EffectiveToUtc,
                Reason = h.Reason,
                ChangedByUserId = h.ChangedByUserId,
                ChangedByUserName = h.ChangedByUserName,
                ChangedByEmail = h.ChangedByEmail,
                CreatedAtUtc = h.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var departments = await _departmentHistoryRepository.GetQueryable()
            .Where(h => h.EmployeeId == employeeId)
            .OrderBy(h => h.EffectiveFromUtc)
            .ThenBy(h => h.Id)
            .Select(h => new DepartmentHistoryItemResponseModel
            {
                Id = h.Id,
                PreviousDepartmentId = h.PreviousDepartmentId,
                NewDepartmentId = h.NewDepartmentId,
                EffectiveFromUtc = h.EffectiveFromUtc,
                EffectiveToUtc = h.EffectiveToUtc,
                Reason = h.Reason,
                ChangedByUserId = h.ChangedByUserId,
                ChangedByUserName = h.ChangedByUserName,
                ChangedByEmail = h.ChangedByEmail,
                CreatedAtUtc = h.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var managers = await _managerHistoryRepository.GetQueryable()
            .Where(h => h.EmployeeId == employeeId)
            .OrderBy(h => h.EffectiveFromUtc)
            .ThenBy(h => h.Id)
            .Select(h => new ManagerHistoryItemResponseModel
            {
                Id = h.Id,
                PreviousManagerId = h.PreviousManagerId,
                NewManagerId = h.NewManagerId,
                EffectiveFromUtc = h.EffectiveFromUtc,
                EffectiveToUtc = h.EffectiveToUtc,
                Reason = h.Reason,
                ChangedByUserId = h.ChangedByUserId,
                ChangedByUserName = h.ChangedByUserName,
                ChangedByEmail = h.ChangedByEmail,
                CreatedAtUtc = h.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return new EmployeeHistoryResponseModel
        {
            EmployeeId = employeeId,
            PositionHistory = positions,
            DepartmentHistory = departments,
            ManagerHistory = managers,
        };
    }

    private async Task EnsureJobPositionMatchesOrganizationAsync(int organizationId, int? jobPositionId, CancellationToken cancellationToken)
    {
        if (jobPositionId is null)
            return;

        var jobPosition = await _jobPositionRepository.GetQueryable()
            .FirstOrDefaultAsync(j => j.Id == jobPositionId.Value, cancellationToken);

        if (jobPosition is null)
            throw new BusinessRuleException("Job position was not found.");

        if (jobPosition.OrganizationId != organizationId)
            throw new BusinessRuleException("Job position must belong to the same organization as the employee.");
    }

    private async Task AddHistoryEntriesAsync(
        Employee entity,
        CreateEmployeeRequestModel request,
        int? previousDepartmentId,
        int? previousJobPositionId,
        int? previousManagerId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var actor = _identityContext.GetCurrent();

        if (previousJobPositionId != entity.JobPositionId && entity.JobPositionId is not null)
        {
            await _positionHistoryRepository.AddAsync(
                new EmployeePositionHistory
                {
                    Employee = entity,
                    PreviousJobPositionId = previousJobPositionId,
                    NewJobPositionId = entity.JobPositionId,
                    EffectiveFromUtc = request.PositionEffectiveFromUtc ?? now,
                    Reason = StringHelper.NormalizeOptional(request.PositionChangeReason),
                    ChangedByUserId = actor.UserId,
                    ChangedByUserName = actor.UserName,
                    ChangedByEmail = actor.Email,
                    CreatedAtUtc = now,
                },
                cancellationToken);
        }

        if (previousDepartmentId != entity.DepartmentId && entity.DepartmentId is not null)
        {
            await _departmentHistoryRepository.AddAsync(
                new EmployeeDepartmentHistory
                {
                    Employee = entity,
                    PreviousDepartmentId = previousDepartmentId,
                    NewDepartmentId = entity.DepartmentId,
                    EffectiveFromUtc = request.DepartmentEffectiveFromUtc ?? now,
                    Reason = StringHelper.NormalizeOptional(request.DepartmentChangeReason),
                    ChangedByUserId = actor.UserId,
                    ChangedByUserName = actor.UserName,
                    ChangedByEmail = actor.Email,
                    CreatedAtUtc = now,
                },
                cancellationToken);
        }

        if (previousManagerId != entity.ManagerId && entity.ManagerId is not null)
        {
            await _managerHistoryRepository.AddAsync(
                new EmployeeManagerHistory
                {
                    Employee = entity,
                    PreviousManagerId = previousManagerId,
                    NewManagerId = entity.ManagerId,
                    EffectiveFromUtc = request.ManagerEffectiveFromUtc ?? now,
                    Reason = StringHelper.NormalizeOptional(request.ManagerChangeReason),
                    ChangedByUserId = actor.UserId,
                    ChangedByUserName = actor.UserName,
                    ChangedByEmail = actor.Email,
                    CreatedAtUtc = now,
                },
                cancellationToken);
        }
    }

    private async Task AddHistoryEntriesAsync(
        Employee entity,
        UpdateEmployeeRequestModel request,
        int? previousDepartmentId,
        int? previousJobPositionId,
        int? previousManagerId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var actor = _identityContext.GetCurrent();

        if (previousJobPositionId != entity.JobPositionId)
        {
            await _positionHistoryRepository.AddAsync(
                new EmployeePositionHistory
                {
                    EmployeeId = entity.Id,
                    PreviousJobPositionId = previousJobPositionId,
                    NewJobPositionId = entity.JobPositionId,
                    EffectiveFromUtc = request.PositionEffectiveFromUtc ?? now,
                    Reason = StringHelper.NormalizeOptional(request.PositionChangeReason),
                    ChangedByUserId = actor.UserId,
                    ChangedByUserName = actor.UserName,
                    ChangedByEmail = actor.Email,
                    CreatedAtUtc = now,
                },
                cancellationToken);
        }

        if (previousDepartmentId != entity.DepartmentId)
        {
            await _departmentHistoryRepository.AddAsync(
                new EmployeeDepartmentHistory
                {
                    EmployeeId = entity.Id,
                    PreviousDepartmentId = previousDepartmentId,
                    NewDepartmentId = entity.DepartmentId,
                    EffectiveFromUtc = request.DepartmentEffectiveFromUtc ?? now,
                    Reason = StringHelper.NormalizeOptional(request.DepartmentChangeReason),
                    ChangedByUserId = actor.UserId,
                    ChangedByUserName = actor.UserName,
                    ChangedByEmail = actor.Email,
                    CreatedAtUtc = now,
                },
                cancellationToken);
        }

        if (previousManagerId != entity.ManagerId)
        {
            await _managerHistoryRepository.AddAsync(
                new EmployeeManagerHistory
                {
                    EmployeeId = entity.Id,
                    PreviousManagerId = previousManagerId,
                    NewManagerId = entity.ManagerId,
                    EffectiveFromUtc = request.ManagerEffectiveFromUtc ?? now,
                    Reason = StringHelper.NormalizeOptional(request.ManagerChangeReason),
                    ChangedByUserId = actor.UserId,
                    ChangedByUserName = actor.UserName,
                    ChangedByEmail = actor.Email,
                    CreatedAtUtc = now,
                },
                cancellationToken);
        }
    }

    private async Task ApplyRetentionFromPolicyAsync(Employee entity, DateTime now, CancellationToken cancellationToken)
    {
        var retentionDays = await _retentionPolicyRepository.GetQueryable()
            .Where(x => x.IsEnabled && (x.OrganizationId == entity.OrganizationId || x.OrganizationId == null))
            .OrderByDescending(x => x.OrganizationId.HasValue)
            .ThenByDescending(x => x.Id)
            .Select(x => x.RetentionDays)
            .FirstOrDefaultAsync(cancellationToken);

        if (retentionDays <= 0)
            retentionDays = 3650;

        entity.RetentionUntilUtc = now.AddDays(retentionDays);
    }

    /// <summary>
    /// Next available code in the form EMP001, EMP002, … per organization (based on existing EMP+digits numbers).
    /// </summary>
    private async Task<string> AllocateNextEmployeeNumberAsync(int organizationId, CancellationToken cancellationToken)
    {
        var rawNumbers = await _repository.GetQueryable()
            .Where(e => e.OrganizationId == organizationId)
            .Select(e => e.EmployeeNumber)
            .ToListAsync(cancellationToken);

        var max = 0;
        foreach (var raw in rawNumbers)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var m = EmployeeNumberSequence.Match(raw.Trim());
            if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                max = Math.Max(max, n);
        }

        return $"EMP{(max + 1).ToString("D3", CultureInfo.InvariantCulture)}";
    }
}
