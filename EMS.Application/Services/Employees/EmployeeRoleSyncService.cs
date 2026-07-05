using System.Globalization;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeRoleSyncService : IEmployeeRoleSyncService
{
    private readonly IBaseRepository<Employee> _employeeRepository;
    private readonly IBaseRepository<PositionRole> _positionRoleRepository;
    private readonly IBaseRepository<EmployeeRoleAssignment> _employeeRoleAssignmentRepository;
    private readonly IEmployeeUserManagementGateway _gateway;

    public EmployeeRoleSyncService(
        IBaseRepository<Employee> employeeRepository,
        IBaseRepository<PositionRole> positionRoleRepository,
        IBaseRepository<EmployeeRoleAssignment> employeeRoleAssignmentRepository,
        IEmployeeUserManagementGateway gateway)
    {
        _employeeRepository = employeeRepository;
        _positionRoleRepository = positionRoleRepository;
        _employeeRoleAssignmentRepository = employeeRoleAssignmentRepository;
        _gateway = gateway;
    }

    public async Task SyncEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null || employee.IsArchived)
            return;

        var desiredInheritedRoleKeys = await GetPositionRoleKeysAsync(employee.JobPositionId, cancellationToken);
        var assignments = await _employeeRoleAssignmentRepository.GetQueryable()
            .Where(x => x.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var inheritedAssignments = assignments.Where(x => x.Source == EmployeeRoleSource.PositionInherited).ToList();
        var directRoleKeys = assignments
            .Where(x => x.Source == EmployeeRoleSource.DirectOverride)
            .Select(x => NormalizeKey(x.RoleKey))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var hasChanges = false;
        var expectedPositionId = employee.JobPositionId;

        foreach (var assignment in inheritedAssignments)
        {
            var shouldKeep = assignment.JobPositionId == expectedPositionId
                && desiredInheritedRoleKeys.Contains(NormalizeKey(assignment.RoleKey));
            if (shouldKeep)
                continue;

            _employeeRoleAssignmentRepository.Remove(assignment);
            hasChanges = true;
        }

        var existingInheritedRoleKeys = inheritedAssignments
            .Where(x => x.JobPositionId == expectedPositionId)
            .Select(x => NormalizeKey(x.RoleKey))
            .ToHashSet(StringComparer.Ordinal);

        if (expectedPositionId is int currentPositionId)
        {
            foreach (var roleKey in desiredInheritedRoleKeys)
            {
                if (existingInheritedRoleKeys.Contains(roleKey))
                    continue;

                await _employeeRoleAssignmentRepository.AddAsync(
                    new EmployeeRoleAssignment
                    {
                        EmployeeId = employeeId,
                        RoleKey = roleKey,
                        Source = EmployeeRoleSource.PositionInherited,
                        JobPositionId = currentPositionId,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow,
                    },
                    cancellationToken);

                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _employeeRoleAssignmentRepository.SaveChangesAsync(cancellationToken);
            assignments = await _employeeRoleAssignmentRepository.GetQueryable()
                .Where(x => x.EmployeeId == employeeId)
                .ToListAsync(cancellationToken);
        }

        var effectiveRoleKeys = assignments
            .Select(x => NormalizeKey(x.RoleKey))
            .Concat(directRoleKeys)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x)
            .ToList();

        await SyncLinkedUserRolesAsync(employee, effectiveRoleKeys, cancellationToken);
    }

    public async Task SyncEmployeesForPositionAsync(int jobPositionId, CancellationToken cancellationToken = default)
    {
        var employeeIds = await _employeeRepository.GetQueryable()
            .Where(x => x.JobPositionId == jobPositionId && !x.IsArchived)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var employeeId in employeeIds)
        {
            await SyncEmployeeAsync(employeeId, cancellationToken);
        }
    }

    private async Task<HashSet<string>> GetPositionRoleKeysAsync(int? jobPositionId, CancellationToken cancellationToken)
    {
        if (jobPositionId is null)
            return new HashSet<string>(StringComparer.Ordinal);

        var roleKeys = await _positionRoleRepository.GetQueryable()
            .Where(x => x.JobPositionId == jobPositionId.Value)
            .Select(x => x.RoleKey)
            .Distinct()
            .ToListAsync(cancellationToken);

        return roleKeys
            .Select(NormalizeKey)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task SyncLinkedUserRolesAsync(
        Employee employee,
        IReadOnlyList<string> effectiveRoleKeys,
        CancellationToken cancellationToken)
    {
        var linkedUser = await EmployeeLinkedIdentityHelper.TryResolveLinkedUserAsync(employee, _gateway, cancellationToken);
        if (linkedUser is null)
            return;

        var externalKey = linkedUser.Id.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(employee.ExternalIdentityKey, externalKey, StringComparison.Ordinal))
        {
            await EmployeeLinkedIdentityHelper.EnsureNoOtherActiveEmployeeUsesExternalIdentityKeyAsync(
                _employeeRepository,
                employee.Id,
                externalKey,
                cancellationToken);
            employee.ExternalIdentityKey = externalKey;
            _employeeRepository.Update(employee);
            await _employeeRepository.SaveChangesAsync(cancellationToken);
        }

        var idempotencyKey = $"sync-roles:employee:{employee.Id}";
        await _gateway.SetRoleKeysForUserAsync(linkedUser.Id, effectiveRoleKeys, idempotencyKey, cancellationToken);
    }

    private static string NormalizeKey(string? roleKey)
        => string.IsNullOrWhiteSpace(roleKey) ? string.Empty : roleKey.Trim().ToUpperInvariant();
}
