using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeScheduledChangeApplier : IEmployeeScheduledChangeApplier
{
    private readonly IBaseRepository<EmployeeScheduledChange> _scheduledChanges;
    private readonly IBaseRepository<Employee> _employees;
    private readonly IBaseRepository<EmployeeEmploymentStatusHistory> _statusHistory;
    private readonly IBaseRepository<EmployeeRetentionPolicy> _retentionPolicies;
    private readonly IBaseRepository<EmployeeDepartmentHistory> _departmentHistory;
    private readonly IBaseRepository<EmployeePositionHistory> _positionHistory;
    private readonly IBaseRepository<EmployeeManagerHistory> _managerHistory;
    private readonly IEmployeeUserManagementGateway _gateway;
    private readonly IEmployeeRoleSyncService _roleSyncService;
    private readonly EmployeeRelationshipValidator _validator;

    public EmployeeScheduledChangeApplier(
        IBaseRepository<EmployeeScheduledChange> scheduledChanges,
        IBaseRepository<Employee> employees,
        IBaseRepository<EmployeeEmploymentStatusHistory> statusHistory,
        IBaseRepository<EmployeeRetentionPolicy> retentionPolicies,
        IBaseRepository<EmployeeDepartmentHistory> departmentHistory,
        IBaseRepository<EmployeePositionHistory> positionHistory,
        IBaseRepository<EmployeeManagerHistory> managerHistory,
        IEmployeeUserManagementGateway gateway,
        IEmployeeRoleSyncService roleSyncService,
        EmployeeRelationshipValidator validator)
    {
        _scheduledChanges = scheduledChanges;
        _employees = employees;
        _statusHistory = statusHistory;
        _retentionPolicies = retentionPolicies;
        _departmentHistory = departmentHistory;
        _positionHistory = positionHistory;
        _managerHistory = managerHistory;
        _gateway = gateway;
        _roleSyncService = roleSyncService;
        _validator = validator;
    }

    public async Task<int> ApplyDueChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueIds = await _scheduledChanges.GetQueryable()
            .AsNoTracking()
            .Where(c => c.Status == EmployeeScheduledChangeStatus.Pending && c.EffectiveAtUtc <= now)
            .OrderBy(c => c.EffectiveAtUtc)
            .Select(c => c.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        var applied = 0;
        foreach (var id in dueIds)
        {
            var change = await _scheduledChanges.GetQueryable()
                .FirstOrDefaultAsync(c => c.Id == id && c.Status == EmployeeScheduledChangeStatus.Pending, cancellationToken);
            if (change is null)
                continue;

            change.Status = EmployeeScheduledChangeStatus.Processing;
            _scheduledChanges.Update(change);
            try
            {
                await _scheduledChanges.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                continue;
            }

            try
            {
                await ApplySingleAsync(change, cancellationToken);
                change.Status = EmployeeScheduledChangeStatus.Applied;
                change.ProcessedAtUtc = DateTime.UtcNow;
                change.FailureReason = null;
                applied++;
            }
            catch (Exception ex)
            {
                change.Status = EmployeeScheduledChangeStatus.Failed;
                change.ProcessedAtUtc = DateTime.UtcNow;
                change.FailureReason = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            }

            _scheduledChanges.Update(change);
            await _scheduledChanges.SaveChangesAsync(cancellationToken);
        }

        return applied;
    }

    private async Task ApplySingleAsync(EmployeeScheduledChange change, CancellationToken cancellationToken)
    {
        var employee = await _employees.GetByIdAsync(change.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("Employee was not found.");

        switch (change.ChangeType)
        {
            case EmployeeScheduledChangeType.Terminate:
                await ApplyTerminateAsync(employee, change, cancellationToken);
                break;
            case EmployeeScheduledChangeType.Archive:
                await ApplyArchiveAsync(employee, change, cancellationToken);
                break;
            case EmployeeScheduledChangeType.ChangeEmploymentStatus:
                await ApplyStatusChangeAsync(employee, change, cancellationToken);
                break;
            case EmployeeScheduledChangeType.TransferDepartment:
                await ApplyDepartmentTransferAsync(employee, change, cancellationToken);
                break;
            case EmployeeScheduledChangeType.TransferPosition:
                await ApplyPositionTransferAsync(employee, change, cancellationToken);
                break;
            case EmployeeScheduledChangeType.TransferManager:
                await ApplyManagerTransferAsync(employee, change, cancellationToken);
                break;
            default:
                throw new BusinessRuleException($"Unsupported scheduled change type {change.ChangeType}.");
        }
    }

    private async Task ApplyTerminateAsync(Employee employee, EmployeeScheduledChange change, CancellationToken cancellationToken)
    {
        if (employee.EmploymentStatus == EmploymentStatus.Terminated)
            return;

        var previous = employee.EmploymentStatus;
        employee.EmploymentStatus = EmploymentStatus.Terminated;
        employee.IsActive = false;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await ApplyRetentionAsync(employee, DateTime.UtcNow, cancellationToken);
        await EmployeeLinkedIdentityHelper.RevokeLinkedIdentityAsync(employee, _gateway, cancellationToken);
        await AddStatusHistoryAsync(employee, previous, EmploymentStatus.Terminated, change.EffectiveAtUtc, change.Reason, change, cancellationToken);

        _employees.Update(employee);
        await _employees.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyArchiveAsync(Employee employee, EmployeeScheduledChange change, CancellationToken cancellationToken)
    {
        if (employee.IsArchived)
            return;

        var now = DateTime.UtcNow;
        employee.IsArchived = true;
        employee.ArchivedAtUtc = now;
        employee.ArchiveReason = change.Reason;
        employee.UpdatedAtUtc = now;

        await ApplyRetentionAsync(employee, now, cancellationToken);
        await EmployeeLinkedIdentityHelper.RevokeLinkedIdentityAsync(employee, _gateway, cancellationToken);

        _employees.Update(employee);
        await _employees.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyStatusChangeAsync(Employee employee, EmployeeScheduledChange change, CancellationToken cancellationToken)
    {
        if (!change.TargetStatus.HasValue)
            throw new BusinessRuleException("Target status is required.");

        var next = (EmploymentStatus)change.TargetStatus.Value;
        if (employee.EmploymentStatus == next)
            return;

        var previous = employee.EmploymentStatus;
        employee.EmploymentStatus = next;
        employee.IsActive = next == EmploymentStatus.Active;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await AddStatusHistoryAsync(employee, previous, next, change.EffectiveAtUtc, change.Reason, change, cancellationToken);

        _employees.Update(employee);
        await _employees.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyDepartmentTransferAsync(Employee employee, EmployeeScheduledChange change, CancellationToken cancellationToken)
    {
        var newValue = change.TargetReferenceId;
        if (employee.DepartmentId == newValue)
            return;

        await CloseOpenDepartmentHistoryAsync(employee.Id, change.EffectiveAtUtc, cancellationToken);
        var previous = employee.DepartmentId;
        employee.DepartmentId = newValue;
        employee.UpdatedAtUtc = DateTime.UtcNow;
        await AddDepartmentHistoryAsync(employee, previous, newValue, change, cancellationToken);
        _employees.Update(employee);
        await _employees.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyPositionTransferAsync(Employee employee, EmployeeScheduledChange change, CancellationToken cancellationToken)
    {
        var newValue = change.TargetReferenceId;
        if (employee.JobPositionId == newValue)
            return;

        await CloseOpenPositionHistoryAsync(employee.Id, change.EffectiveAtUtc, cancellationToken);
        var previous = employee.JobPositionId;
        employee.JobPositionId = newValue;
        employee.UpdatedAtUtc = DateTime.UtcNow;
        await AddPositionHistoryAsync(employee, previous, newValue, change, cancellationToken);
        _employees.Update(employee);
        await _employees.SaveChangesAsync(cancellationToken);
        await _roleSyncService.SyncEmployeeAsync(employee.Id, cancellationToken);
    }

    private async Task ApplyManagerTransferAsync(Employee employee, EmployeeScheduledChange change, CancellationToken cancellationToken)
    {
        var newValue = change.TargetReferenceId;
        if (employee.ManagerId == newValue)
            return;

        await CloseOpenManagerHistoryAsync(employee.Id, change.EffectiveAtUtc, cancellationToken);
        var previous = employee.ManagerId;
        employee.ManagerId = newValue;
        employee.UpdatedAtUtc = DateTime.UtcNow;
        await AddManagerHistoryAsync(employee, previous, newValue, change, cancellationToken);
        _employees.Update(employee);
        await _employees.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyRetentionAsync(Employee employee, DateTime now, CancellationToken cancellationToken)
    {
        var retentionDays = await _retentionPolicies.GetQueryable()
            .Where(x => x.IsEnabled && (x.OrganizationId == employee.OrganizationId || x.OrganizationId == null))
            .OrderByDescending(x => x.OrganizationId.HasValue)
            .ThenByDescending(x => x.Id)
            .Select(x => x.RetentionDays)
            .FirstOrDefaultAsync(cancellationToken);

        if (retentionDays <= 0)
            retentionDays = 3650;

        employee.RetentionUntilUtc = now.AddDays(retentionDays);
    }

    private async Task AddStatusHistoryAsync(
        Employee employee,
        EmploymentStatus? previous,
        EmploymentStatus next,
        DateTime effectiveDate,
        string? reason,
        EmployeeScheduledChange change,
        CancellationToken cancellationToken)
    {
        await _statusHistory.AddAsync(
            new EmployeeEmploymentStatusHistory
            {
                Employee = employee,
                PreviousStatus = previous,
                NewStatus = next,
                EffectiveDateUtc = effectiveDate,
                Reason = reason,
                ChangedByUserId = change.CreatedByUserId,
                ChangedByUserName = change.CreatedByUserName,
                ChangedByEmail = change.CreatedByEmail,
                CreatedAtUtc = DateTime.UtcNow,
            },
            cancellationToken);
    }

    private async Task CloseOpenDepartmentHistoryAsync(int employeeId, DateTime effectiveToUtc, CancellationToken cancellationToken)
    {
        var open = await _departmentHistory.GetQueryable()
            .Where(h => h.EmployeeId == employeeId && h.EffectiveToUtc == null)
            .OrderByDescending(h => h.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (open is not null)
        {
            open.EffectiveToUtc = effectiveToUtc;
            _departmentHistory.Update(open);
        }
    }

    private async Task CloseOpenPositionHistoryAsync(int employeeId, DateTime effectiveToUtc, CancellationToken cancellationToken)
    {
        var open = await _positionHistory.GetQueryable()
            .Where(h => h.EmployeeId == employeeId && h.EffectiveToUtc == null)
            .OrderByDescending(h => h.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (open is not null)
        {
            open.EffectiveToUtc = effectiveToUtc;
            _positionHistory.Update(open);
        }
    }

    private async Task CloseOpenManagerHistoryAsync(int employeeId, DateTime effectiveToUtc, CancellationToken cancellationToken)
    {
        var open = await _managerHistory.GetQueryable()
            .Where(h => h.EmployeeId == employeeId && h.EffectiveToUtc == null)
            .OrderByDescending(h => h.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (open is not null)
        {
            open.EffectiveToUtc = effectiveToUtc;
            _managerHistory.Update(open);
        }
    }

    private Task AddDepartmentHistoryAsync(
        Employee employee,
        int? previous,
        int? next,
        EmployeeScheduledChange change,
        CancellationToken cancellationToken)
        => _departmentHistory.AddAsync(
            new EmployeeDepartmentHistory
            {
                Employee = employee,
                PreviousDepartmentId = previous,
                NewDepartmentId = next,
                EffectiveFromUtc = change.EffectiveAtUtc,
                Reason = change.Reason,
                ChangedByUserId = change.CreatedByUserId,
                ChangedByUserName = change.CreatedByUserName,
                ChangedByEmail = change.CreatedByEmail,
                CreatedAtUtc = DateTime.UtcNow,
            },
            cancellationToken);

    private Task AddPositionHistoryAsync(
        Employee employee,
        int? previous,
        int? next,
        EmployeeScheduledChange change,
        CancellationToken cancellationToken)
        => _positionHistory.AddAsync(
            new EmployeePositionHistory
            {
                Employee = employee,
                PreviousJobPositionId = previous,
                NewJobPositionId = next,
                EffectiveFromUtc = change.EffectiveAtUtc,
                Reason = change.Reason,
                ChangedByUserId = change.CreatedByUserId,
                ChangedByUserName = change.CreatedByUserName,
                ChangedByEmail = change.CreatedByEmail,
                CreatedAtUtc = DateTime.UtcNow,
            },
            cancellationToken);

    private Task AddManagerHistoryAsync(
        Employee employee,
        int? previous,
        int? next,
        EmployeeScheduledChange change,
        CancellationToken cancellationToken)
        => _managerHistory.AddAsync(
            new EmployeeManagerHistory
            {
                Employee = employee,
                PreviousManagerId = previous,
                NewManagerId = next,
                EffectiveFromUtc = change.EffectiveAtUtc,
                Reason = change.Reason,
                ChangedByUserId = change.CreatedByUserId,
                ChangedByUserName = change.CreatedByUserName,
                ChangedByEmail = change.CreatedByEmail,
                CreatedAtUtc = DateTime.UtcNow,
            },
            cancellationToken);
}
