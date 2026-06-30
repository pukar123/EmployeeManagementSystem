using EMS.Application.DTOs.Employee;
using EMS.Application.Mapping;
using EMS.Application.Services.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeTransferService : IEmployeeTransferService
{
    private readonly IBaseRepository<Employee> _employees;
    private readonly IBaseRepository<EmployeePositionHistory> _positionHistoryRepository;
    private readonly IBaseRepository<EmployeeDepartmentHistory> _departmentHistoryRepository;
    private readonly IBaseRepository<EmployeeManagerHistory> _managerHistoryRepository;
    private readonly IIdentityContext _identityContext;
    private readonly IEmployeeRoleSyncService _employeeRoleSyncService;
    private readonly EmployeeRelationshipValidator _validator;

    public EmployeeTransferService(
        IBaseRepository<Employee> employees,
        IBaseRepository<EmployeePositionHistory> positionHistoryRepository,
        IBaseRepository<EmployeeDepartmentHistory> departmentHistoryRepository,
        IBaseRepository<EmployeeManagerHistory> managerHistoryRepository,
        IIdentityContext identityContext,
        IEmployeeRoleSyncService employeeRoleSyncService,
        EmployeeRelationshipValidator validator)
    {
        _employees = employees;
        _positionHistoryRepository = positionHistoryRepository;
        _departmentHistoryRepository = departmentHistoryRepository;
        _managerHistoryRepository = managerHistoryRepository;
        _identityContext = identityContext;
        _employeeRoleSyncService = employeeRoleSyncService;
        _validator = validator;
    }

    public Task<EmployeeResponseModel?> TransferDepartmentAsync(
        int employeeId,
        TransferEmployeeDepartmentRequestModel request,
        CancellationToken cancellationToken = default)
        => TransferAsync(
            employeeId,
            request.NewDepartmentId,
            request.EffectiveFromUtc,
            request.Reason,
            TransferKind.Department,
            cancellationToken);

    public Task<EmployeeResponseModel?> TransferPositionAsync(
        int employeeId,
        TransferEmployeePositionRequestModel request,
        CancellationToken cancellationToken = default)
        => TransferAsync(
            employeeId,
            request.NewJobPositionId,
            request.EffectiveFromUtc,
            request.Reason,
            TransferKind.Position,
            cancellationToken);

    public Task<EmployeeResponseModel?> TransferManagerAsync(
        int employeeId,
        TransferEmployeeManagerRequestModel request,
        CancellationToken cancellationToken = default)
        => TransferAsync(
            employeeId,
            request.NewManagerId,
            request.EffectiveFromUtc,
            request.Reason,
            TransferKind.Manager,
            cancellationToken);

    private async Task<EmployeeResponseModel?> TransferAsync(
        int employeeId,
        int? newValue,
        DateTime effectiveFromUtc,
        string? reason,
        TransferKind kind,
        CancellationToken cancellationToken)
    {
        var entity = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (entity is null)
            return null;

        _validator.EnsureNotArchived(entity);

        var normalizedReason = StringHelper.NormalizeOptional(reason);
        var effectiveFrom = effectiveFromUtc == default ? DateTime.UtcNow : effectiveFromUtc;
        var now = DateTime.UtcNow;

        await using var transaction = await _employees.BeginTransactionAsync(cancellationToken);
        try
        {
            switch (kind)
            {
                case TransferKind.Department:
                {
                    var previous = entity.DepartmentId;
                    if (previous == newValue)
                        throw new BusinessRuleException("Department transfer does not change the current department.");

                    if (newValue is int deptId)
                        await _validator.ValidateRelationshipsForCreateOrUpdateAsync(
                            employeeId,
                            entity.OrganizationId,
                            deptId,
                            entity.LocationId,
                            entity.JobPositionId,
                            entity.ManagerId,
                            entity.Email,
                            cancellationToken);

                    await CloseOpenDepartmentHistoryAsync(employeeId, effectiveFrom, cancellationToken);
                    entity.DepartmentId = newValue;
                    await AddDepartmentHistoryAsync(entity, previous, newValue, effectiveFrom, normalizedReason, now, cancellationToken);
                    break;
                }
                case TransferKind.Position:
                {
                    var previous = entity.JobPositionId;
                    if (previous == newValue)
                        throw new BusinessRuleException("Position transfer does not change the current job position.");

                    if (newValue is int posId)
                        await _validator.ValidateRelationshipsForCreateOrUpdateAsync(
                            employeeId,
                            entity.OrganizationId,
                            entity.DepartmentId,
                            entity.LocationId,
                            posId,
                            entity.ManagerId,
                            entity.Email,
                            cancellationToken);

                    await CloseOpenPositionHistoryAsync(employeeId, effectiveFrom, cancellationToken);
                    entity.JobPositionId = newValue;
                    await AddPositionHistoryAsync(entity, previous, newValue, effectiveFrom, normalizedReason, now, cancellationToken);
                    break;
                }
                case TransferKind.Manager:
                {
                    var previous = entity.ManagerId;
                    if (previous == newValue)
                        throw new BusinessRuleException("Manager transfer does not change the current manager.");

                    if (newValue is int mgrId)
                        await _validator.ValidateRelationshipsForCreateOrUpdateAsync(
                            employeeId,
                            entity.OrganizationId,
                            entity.DepartmentId,
                            entity.LocationId,
                            entity.JobPositionId,
                            mgrId,
                            entity.Email,
                            cancellationToken);

                    await CloseOpenManagerHistoryAsync(employeeId, effectiveFrom, cancellationToken);
                    entity.ManagerId = newValue;
                    await AddManagerHistoryAsync(entity, previous, newValue, effectiveFrom, normalizedReason, now, cancellationToken);
                    break;
                }
            }

            entity.UpdatedAtUtc = now;
            _employees.Update(entity);
            await _employees.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        if (kind == TransferKind.Position)
            await _employeeRoleSyncService.SyncEmployeeAsync(entity.Id, cancellationToken);

        return EmployeeMapper.ToResponse(entity);
    }

    private async Task CloseOpenPositionHistoryAsync(int employeeId, DateTime effectiveToUtc, CancellationToken cancellationToken)
    {
        var open = await _positionHistoryRepository.GetQueryable()
            .Where(h => h.EmployeeId == employeeId && h.EffectiveToUtc == null)
            .OrderByDescending(h => h.EffectiveFromUtc)
            .ThenByDescending(h => h.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (open is not null)
        {
            open.EffectiveToUtc = effectiveToUtc;
            _positionHistoryRepository.Update(open);
        }
    }

    private async Task CloseOpenDepartmentHistoryAsync(int employeeId, DateTime effectiveToUtc, CancellationToken cancellationToken)
    {
        var open = await _departmentHistoryRepository.GetQueryable()
            .Where(h => h.EmployeeId == employeeId && h.EffectiveToUtc == null)
            .OrderByDescending(h => h.EffectiveFromUtc)
            .ThenByDescending(h => h.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (open is not null)
        {
            open.EffectiveToUtc = effectiveToUtc;
            _departmentHistoryRepository.Update(open);
        }
    }

    private async Task CloseOpenManagerHistoryAsync(int employeeId, DateTime effectiveToUtc, CancellationToken cancellationToken)
    {
        var open = await _managerHistoryRepository.GetQueryable()
            .Where(h => h.EmployeeId == employeeId && h.EffectiveToUtc == null)
            .OrderByDescending(h => h.EffectiveFromUtc)
            .ThenByDescending(h => h.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (open is not null)
        {
            open.EffectiveToUtc = effectiveToUtc;
            _managerHistoryRepository.Update(open);
        }
    }

    private async Task AddPositionHistoryAsync(
        Employee entity,
        int? previous,
        int? current,
        DateTime effectiveFrom,
        string? reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var actor = _identityContext.GetCurrent();
        await _positionHistoryRepository.AddAsync(
            new EmployeePositionHistory
            {
                EmployeeId = entity.Id,
                PreviousJobPositionId = previous,
                NewJobPositionId = current,
                EffectiveFromUtc = effectiveFrom,
                Reason = reason,
                ChangedByUserId = actor.UserId,
                ChangedByUserName = actor.UserName,
                ChangedByEmail = actor.Email,
                CreatedAtUtc = now,
            },
            cancellationToken);
    }

    private async Task AddDepartmentHistoryAsync(
        Employee entity,
        int? previous,
        int? current,
        DateTime effectiveFrom,
        string? reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var actor = _identityContext.GetCurrent();
        await _departmentHistoryRepository.AddAsync(
            new EmployeeDepartmentHistory
            {
                EmployeeId = entity.Id,
                PreviousDepartmentId = previous,
                NewDepartmentId = current,
                EffectiveFromUtc = effectiveFrom,
                Reason = reason,
                ChangedByUserId = actor.UserId,
                ChangedByUserName = actor.UserName,
                ChangedByEmail = actor.Email,
                CreatedAtUtc = now,
            },
            cancellationToken);
    }

    private async Task AddManagerHistoryAsync(
        Employee entity,
        int? previous,
        int? current,
        DateTime effectiveFrom,
        string? reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var actor = _identityContext.GetCurrent();
        await _managerHistoryRepository.AddAsync(
            new EmployeeManagerHistory
            {
                EmployeeId = entity.Id,
                PreviousManagerId = previous,
                NewManagerId = current,
                EffectiveFromUtc = effectiveFrom,
                Reason = reason,
                ChangedByUserId = actor.UserId,
                ChangedByUserName = actor.UserName,
                ChangedByEmail = actor.Email,
                CreatedAtUtc = now,
            },
            cancellationToken);
    }

    private enum TransferKind
    {
        Department,
        Position,
        Manager,
    }
}
