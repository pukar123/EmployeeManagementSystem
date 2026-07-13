using EMS.Application.DTOs.Employee;
using Pukar.Shared;
using EMS.Application.Mapping;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Integrations;
using EMS.Application.Services.Onboarding;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeService : IEmployeeService
{
    private readonly IBaseRepository<Employee> _repository;
    private readonly IBaseRepository<EmployeePositionHistory> _positionHistoryRepository;
    private readonly IBaseRepository<EmployeeDepartmentHistory> _departmentHistoryRepository;
    private readonly IBaseRepository<EmployeeManagerHistory> _managerHistoryRepository;
    private readonly IBaseRepository<EmployeeEmploymentStatusHistory> _statusHistoryRepository;
    private readonly IBaseRepository<EmployeeRetentionPolicy> _retentionPolicyRepository;
    private readonly IBaseRepository<JobPosition> _jobPositions;
    private readonly IBaseRepository<Department> _departments;
    private readonly IIdentityContext _identityContext;
    private readonly IEmployeeRoleSyncService _employeeRoleSyncService;
    private readonly IEmployeeNumberAllocator _employeeNumberAllocator;
    private readonly IEmployeeUserManagementGateway _gateway;
    private readonly IIntegrationOutboxWriter _outboxWriter;
    private readonly EmployeeRelationshipValidator _validator;
    private readonly IOnboardingChecklistService _onboardingChecklistService;

    public EmployeeService(
        IBaseRepository<Employee> repository,
        IBaseRepository<EmployeePositionHistory> positionHistoryRepository,
        IBaseRepository<EmployeeDepartmentHistory> departmentHistoryRepository,
        IBaseRepository<EmployeeManagerHistory> managerHistoryRepository,
        IBaseRepository<EmployeeEmploymentStatusHistory> statusHistoryRepository,
        IBaseRepository<EmployeeRetentionPolicy> retentionPolicyRepository,
        IBaseRepository<JobPosition> jobPositions,
        IBaseRepository<Department> departments,
        IIdentityContext identityContext,
        IEmployeeRoleSyncService employeeRoleSyncService,
        IEmployeeNumberAllocator employeeNumberAllocator,
        IEmployeeUserManagementGateway gateway,
        IIntegrationOutboxWriter outboxWriter,
        EmployeeRelationshipValidator validator,
        IOnboardingChecklistService onboardingChecklistService)
    {
        _repository = repository;
        _positionHistoryRepository = positionHistoryRepository;
        _departmentHistoryRepository = departmentHistoryRepository;
        _managerHistoryRepository = managerHistoryRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _retentionPolicyRepository = retentionPolicyRepository;
        _jobPositions = jobPositions;
        _departments = departments;
        _identityContext = identityContext;
        _employeeRoleSyncService = employeeRoleSyncService;
        _employeeNumberAllocator = employeeNumberAllocator;
        _gateway = gateway;
        _outboxWriter = outboxWriter;
        _validator = validator;
        _onboardingChecklistService = onboardingChecklistService;
    }

    public async Task<EmployeeResponseModel> CreateAsync(CreateEmployeeRequestModel request, CancellationToken cancellationToken = default)
    {
        EmployeeMapper.NormalizeCreateRequest(request);
        var (normalizedEmail, normalizedPhone) = _validator.NormalizeAndValidateProfile(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.DateOfBirth,
            request.DateJoined,
            request.EmploymentStatus);
        request.Email = normalizedEmail;
        request.PhoneNumber = normalizedPhone;

        await _validator.ValidateRelationshipsForCreateOrUpdateAsync(
            employeeId: 0,
            request.OrganizationId,
            request.DepartmentId,
            request.LocationId,
            request.JobPositionId,
            request.ManagerId,
            request.Email,
            cancellationToken);

        var now = DateTime.UtcNow;

        await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);
        try
        {
            var entity = EmployeeMapper.ToEntity(request);
            entity.EmployeeNumber = await _employeeNumberAllocator.AllocateAsync(request.OrganizationId, cancellationToken);
            entity.CreatedAtUtc = now;
            entity.UpdatedAtUtc = now;

            await _repository.AddAsync(entity, cancellationToken);
            await AddCreateHistoryEntriesAsync(entity, request, now, cancellationToken);
            await AddInitialStatusHistoryAsync(entity, null, entity.EmploymentStatus, request.DateJoined, "Employee created.", now, cancellationToken);

            if (entity.EmploymentStatus == EmploymentStatus.Terminated)
                await ApplyRetentionFromPolicyAsync(entity, now, cancellationToken);

            if (request.GenerateOnboardingTasks
                && request.OnboardingTemplateId.HasValue
                && entity.EmploymentStatus == EmploymentStatus.Preboarding)
            {
                var assignedByUserId = _identityContext.GetCurrent().UserId;
                await _onboardingChecklistService.GenerateForEmployeeAsync(
                    entity,
                    request.OnboardingTemplateId.Value,
                    assignedByUserId,
                    entity.DateJoined,
                    cancellationToken);
            }

            await _outboxWriter.EnqueueSyncEmployeeRolesAsync(entity.Id, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return EmployeeMapper.ToResponse(entity);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw EmployeePersistenceExceptionMapper.MapDbUpdateException(ex);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<EmployeeResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity?.IsArchived == true)
            return null;
        return entity is null ? null : EmployeeMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<EmployeeResponseModel>> GetAllAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable();
        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var list = await query.ToListAsync(cancellationToken);
        return list.Select(EmployeeMapper.ToResponse).ToList();
    }

    public async Task<EmployeeProfileResponseModel?> GetProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.JobPosition)
            .Include(e => e.Manager)
            .Include(e => e.Location)
            .Include(e => e.EmployeeSites)
            .ThenInclude(es => es.Site)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null)
            return null;

        var sites = entity.EmployeeSites
            .Select(es => es.Site)
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.SiteName)
            .ToList();

        var linkedUser = await EmployeeLinkedIdentityHelper.TryResolveLinkedUserAsync(entity, _gateway, cancellationToken);
        var locationLabel = entity.Location is null
            ? null
            : string.IsNullOrWhiteSpace(entity.Location.Line1)
                ? entity.Location.Name
                : $"{entity.Location.Name} — {entity.Location.Line1}";

        return EmployeeMapper.ToProfile(
            entity,
            entity.Department?.Name,
            entity.JobPosition?.Title,
            entity.JobPosition?.Code,
            entity.Manager is null ? null : $"{entity.Manager.FirstName} {entity.Manager.LastName}",
            entity.Manager?.EmployeeNumber,
            locationLabel,
            sites.Select(s => s.SiteName).ToList(),
            sites.FirstOrDefault()?.SiteName,
            linkedUser is not null,
            linkedUser?.Id,
            linkedUser?.Email,
            linkedUser?.IsActive);
    }

    public async Task<IReadOnlyList<PossibleDuplicateEmployeeModel>> FindPossibleDuplicatesAsync(
        int organizationId,
        string? email,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        DateTime? dateOfBirth,
        CancellationToken cancellationToken = default)
    {
        if (organizationId <= 0)
            throw new BusinessRuleException("OrganizationId is required.");

        var results = new List<PossibleDuplicateEmployeeModel>();
        var seen = new HashSet<int>();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = EmailNormalizer.Normalize(email.Trim());
            var matches = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(e => e.OrganizationId == organizationId && e.Email.ToUpper() == normalizedEmail)
                .ToListAsync(cancellationToken);

            foreach (var match in matches)
            {
                if (seen.Add(match.Id))
                {
                    results.Add(ToDuplicate(match, $"This email is already used by employee {match.EmployeeNumber}."));
                }
            }
        }

        var normalizedPhone = EmployeeRelationshipValidator.NormalizePhoneNumber(phoneNumber);
        if (normalizedPhone is not null)
        {
            var matches = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(e => e.OrganizationId == organizationId && e.PhoneNumber == normalizedPhone)
                .ToListAsync(cancellationToken);

            foreach (var match in matches)
            {
                if (seen.Add(match.Id))
                {
                    results.Add(ToDuplicate(match, $"This phone number is already used by employee {match.EmployeeNumber}."));
                }
            }
        }

        if (dateOfBirth.HasValue
            && !string.IsNullOrWhiteSpace(firstName)
            && !string.IsNullOrWhiteSpace(lastName))
        {
            var fn = StringHelper.NormalizeRequired(firstName);
            var ln = StringHelper.NormalizeRequired(lastName);
            var dob = dateOfBirth.Value.Date;

            var matches = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(e => e.OrganizationId == organizationId
                    && e.FirstName == fn
                    && e.LastName == ln
                    && e.DateOfBirth.Date == dob)
                .ToListAsync(cancellationToken);

            foreach (var match in matches)
            {
                if (seen.Add(match.Id))
                {
                    results.Add(ToDuplicate(match, $"An employee with the same name and date of birth exists ({match.EmployeeNumber})."));
                }
            }
        }

        return results;
    }

    public async Task<EmployeeResponseModel?> UpdateAsync(int id, UpdateEmployeeRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        _validator.EnsureNotArchived(entity);
        _validator.EnsureProfileDoesNotChangeOrganizationalFields(entity, request);

        EmployeeMapper.NormalizeUpdateRequest(request);
        var storedStatus = entity.EmploymentStatus;
        var (normalizedEmail, normalizedPhone) = _validator.NormalizeAndValidateProfile(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.DateOfBirth,
            request.DateJoined,
            storedStatus);
        request.Email = normalizedEmail;
        request.PhoneNumber = normalizedPhone;

        await _validator.ValidateRelationshipsForCreateOrUpdateAsync(
            id,
            request.OrganizationId,
            entity.DepartmentId,
            request.LocationId,
            entity.JobPositionId,
            entity.ManagerId,
            request.Email,
            cancellationToken);

        var now = DateTime.UtcNow;

        EmployeeMapper.ApplyProfileUpdate(entity, request);
        entity.UpdatedAtUtc = now;

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return EmployeeMapper.ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, ArchiveEmployeeRequestModel? request = null, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        if (entity.IsArchived)
            return true;

        var now = DateTime.UtcNow;
        entity.IsArchived = true;
        entity.ArchivedAtUtc = now;
        entity.ArchiveReason = StringHelper.NormalizeOptional(request?.Reason) ?? "Archived via archive request.";
        entity.UpdatedAtUtc = now;

        await ApplyRetentionFromPolicyAsync(entity, now, cancellationToken);
        await EmployeeLinkedIdentityHelper.RevokeLinkedIdentityAsync(
            entity,
            _gateway,
            $"revoke-identity:employee:{entity.Id}",
            cancellationToken);

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

        var positionIds = new HashSet<int>();
        var departmentIds = new HashSet<int>();
        var managerIds = new HashSet<int>();

        var positionsRaw = await _positionHistoryRepository.GetQueryable()
            .Where(h => h.EmployeeId == employeeId)
            .OrderBy(h => h.EffectiveFromUtc)
            .ThenBy(h => h.Id)
            .ToListAsync(cancellationToken);

        var departmentsRaw = await _departmentHistoryRepository.GetQueryable()
            .Where(h => h.EmployeeId == employeeId)
            .OrderBy(h => h.EffectiveFromUtc)
            .ThenBy(h => h.Id)
            .ToListAsync(cancellationToken);

        var managersRaw = await _managerHistoryRepository.GetQueryable()
            .Where(h => h.EmployeeId == employeeId)
            .OrderBy(h => h.EffectiveFromUtc)
            .ThenBy(h => h.Id)
            .ToListAsync(cancellationToken);

        foreach (var h in positionsRaw)
        {
            if (h.PreviousJobPositionId is int p) positionIds.Add(p);
            if (h.NewJobPositionId is int n) positionIds.Add(n);
        }

        foreach (var h in departmentsRaw)
        {
            if (h.PreviousDepartmentId is int p) departmentIds.Add(p);
            if (h.NewDepartmentId is int n) departmentIds.Add(n);
        }

        foreach (var h in managersRaw)
        {
            if (h.PreviousManagerId is int p) managerIds.Add(p);
            if (h.NewManagerId is int n) managerIds.Add(n);
        }

        var positionTitleMap = await LoadJobPositionTitlesAsync(positionIds, cancellationToken);
        var departmentNameMap = await LoadDepartmentNamesAsync(departmentIds, cancellationToken);
        var managerLabelMap = await LoadManagerLabelsAsync(managerIds, cancellationToken);

        var positions = positionsRaw.Select(h => new PositionHistoryItemResponseModel
        {
            Id = h.Id,
            PreviousJobPositionId = h.PreviousJobPositionId,
            PreviousJobPositionTitle = h.PreviousJobPositionId is int pp ? positionTitleMap.GetValueOrDefault(pp) : null,
            NewJobPositionId = h.NewJobPositionId,
            NewJobPositionTitle = h.NewJobPositionId is int np ? positionTitleMap.GetValueOrDefault(np) : null,
            EffectiveFromUtc = h.EffectiveFromUtc,
            EffectiveToUtc = h.EffectiveToUtc,
            Reason = h.Reason,
            ChangedByUserId = h.ChangedByUserId,
            ChangedByUserName = h.ChangedByUserName,
            ChangedByEmail = h.ChangedByEmail,
            CreatedAtUtc = h.CreatedAtUtc,
        }).ToList();

        var departments = departmentsRaw.Select(h => new DepartmentHistoryItemResponseModel
        {
            Id = h.Id,
            PreviousDepartmentId = h.PreviousDepartmentId,
            PreviousDepartmentName = h.PreviousDepartmentId is int pd ? departmentNameMap.GetValueOrDefault(pd) : null,
            NewDepartmentId = h.NewDepartmentId,
            NewDepartmentName = h.NewDepartmentId is int nd ? departmentNameMap.GetValueOrDefault(nd) : null,
            EffectiveFromUtc = h.EffectiveFromUtc,
            EffectiveToUtc = h.EffectiveToUtc,
            Reason = h.Reason,
            ChangedByUserId = h.ChangedByUserId,
            ChangedByUserName = h.ChangedByUserName,
            ChangedByEmail = h.ChangedByEmail,
            CreatedAtUtc = h.CreatedAtUtc,
        }).ToList();

        var managers = managersRaw.Select(h => new ManagerHistoryItemResponseModel
        {
            Id = h.Id,
            PreviousManagerId = h.PreviousManagerId,
            PreviousManagerName = h.PreviousManagerId is int pmId && managerLabelMap.TryGetValue(pmId, out var prev)
                ? prev.Name
                : null,
            PreviousManagerEmployeeNumber = h.PreviousManagerId is int pmId2 && managerLabelMap.TryGetValue(pmId2, out var prev2)
                ? prev2.EmployeeNumber
                : null,
            NewManagerId = h.NewManagerId,
            NewManagerName = h.NewManagerId is int nmId && managerLabelMap.TryGetValue(nmId, out var nm)
                ? nm.Name
                : null,
            NewManagerEmployeeNumber = h.NewManagerId is int nmId2 && managerLabelMap.TryGetValue(nmId2, out var nm2)
                ? nm2.EmployeeNumber
                : null,
            EffectiveFromUtc = h.EffectiveFromUtc,
            EffectiveToUtc = h.EffectiveToUtc,
            Reason = h.Reason,
            ChangedByUserId = h.ChangedByUserId,
            ChangedByUserName = h.ChangedByUserName,
            ChangedByEmail = h.ChangedByEmail,
            CreatedAtUtc = h.CreatedAtUtc,
        }).ToList();

        var statusHistory = await _statusHistoryRepository.GetQueryable()
            .Where(h => h.EmployeeId == employeeId)
            .OrderBy(h => h.EffectiveDateUtc)
            .ThenBy(h => h.Id)
            .Select(h => new EmploymentStatusHistoryItemResponseModel
            {
                Id = h.Id,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                EffectiveDateUtc = h.EffectiveDateUtc,
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
            EmploymentStatusHistory = statusHistory,
        };
    }

    private async Task<Dictionary<int, string>> LoadJobPositionTitlesAsync(HashSet<int> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        return await _jobPositions.GetQueryable()
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Title, cancellationToken);
    }

    private async Task<Dictionary<int, string>> LoadDepartmentNamesAsync(HashSet<int> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        return await _departments.GetQueryable()
            .AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);
    }

    private async Task<Dictionary<int, (string Name, string EmployeeNumber)>> LoadManagerLabelsAsync(
        HashSet<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return new Dictionary<int, (string Name, string EmployeeNumber)>();

        return await _repository.GetQueryable()
            .AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(
                e => e.Id,
                e => ($"{e.FirstName} {e.LastName}", e.EmployeeNumber),
                cancellationToken);
    }

    private static PossibleDuplicateEmployeeModel ToDuplicate(Employee match, string reason)
        => new()
        {
            Id = match.Id,
            EmployeeNumber = match.EmployeeNumber,
            FirstName = match.FirstName,
            LastName = match.LastName,
            Email = match.Email,
            PhoneNumber = match.PhoneNumber,
            DateOfBirth = match.DateOfBirth,
            MatchReason = reason,
        };

    private async Task AddInitialStatusHistoryAsync(
        Employee entity,
        EmploymentStatus? previous,
        EmploymentStatus next,
        DateTime effectiveDate,
        string? reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var actor = _identityContext.GetCurrent();
        await _statusHistoryRepository.AddAsync(
            new EmployeeEmploymentStatusHistory
            {
                Employee = entity,
                PreviousStatus = previous,
                NewStatus = next,
                EffectiveDateUtc = effectiveDate.ToUniversalTime(),
                Reason = reason,
                ChangedByUserId = actor.UserId,
                ChangedByUserName = actor.UserName,
                ChangedByEmail = actor.Email,
                CreatedAtUtc = now,
            },
            cancellationToken);
    }

    private async Task AddCreateHistoryEntriesAsync(
        Employee entity,
        CreateEmployeeRequestModel request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var actor = _identityContext.GetCurrent();

        if (entity.JobPositionId is not null)
        {
            await _positionHistoryRepository.AddAsync(
                new EmployeePositionHistory
                {
                    Employee = entity,
                    PreviousJobPositionId = null,
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

        if (entity.DepartmentId is not null)
        {
            await _departmentHistoryRepository.AddAsync(
                new EmployeeDepartmentHistory
                {
                    Employee = entity,
                    PreviousDepartmentId = null,
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

        if (entity.ManagerId is not null)
        {
            await _managerHistoryRepository.AddAsync(
                new EmployeeManagerHistory
                {
                    Employee = entity,
                    PreviousManagerId = null,
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
}
