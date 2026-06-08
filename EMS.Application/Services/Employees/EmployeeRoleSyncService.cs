using System.Globalization;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

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
        if (employee is null)
            return;

        var desiredInheritedRoleIds = await GetPositionRoleIdsAsync(employee.JobPositionId, cancellationToken);
        var assignments = await _employeeRoleAssignmentRepository.GetQueryable()
            .Where(x => x.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var inheritedAssignments = assignments.Where(x => x.Source == EmployeeRoleSource.PositionInherited).ToList();
        var directRoleIds = assignments
            .Where(x => x.Source == EmployeeRoleSource.DirectOverride)
            .Select(x => x.RoleId)
            .Distinct()
            .ToHashSet();

        var hasChanges = false;
        var expectedPositionId = employee.JobPositionId;

        foreach (var assignment in inheritedAssignments)
        {
            var shouldKeep = assignment.JobPositionId == expectedPositionId
                && desiredInheritedRoleIds.Contains(assignment.RoleId);
            if (shouldKeep)
                continue;

            _employeeRoleAssignmentRepository.Remove(assignment);
            hasChanges = true;
        }

        var existingInheritedRoleIds = inheritedAssignments
            .Where(x => x.JobPositionId == expectedPositionId)
            .Select(x => x.RoleId)
            .ToHashSet();

        if (expectedPositionId is int currentPositionId)
        {
            foreach (var roleId in desiredInheritedRoleIds)
            {
                if (existingInheritedRoleIds.Contains(roleId))
                    continue;

                await _employeeRoleAssignmentRepository.AddAsync(
                    new EmployeeRoleAssignment
                    {
                        EmployeeId = employeeId,
                        RoleId = roleId,
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

        var effectiveRoleIds = assignments
            .Select(x => x.RoleId)
            .Concat(directRoleIds)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        await SyncLinkedUserRolesAsync(employee, effectiveRoleIds, cancellationToken);
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

    private async Task<HashSet<int>> GetPositionRoleIdsAsync(int? jobPositionId, CancellationToken cancellationToken)
    {
        if (jobPositionId is null)
            return new HashSet<int>();

        var roleIds = await _positionRoleRepository.GetQueryable()
            .Where(x => x.JobPositionId == jobPositionId.Value)
            .Select(x => x.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return roleIds.ToHashSet();
    }

    private async Task SyncLinkedUserRolesAsync(Employee employee, IReadOnlyList<int> effectiveRoleIds, CancellationToken cancellationToken)
    {
        var linkedUser = await ResolveLinkedUserAsync(employee, cancellationToken);
        if (linkedUser is null)
            return;

        var externalKey = linkedUser.Id.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(employee.ExternalIdentityKey, externalKey, StringComparison.Ordinal))
        {
            await EnsureNoOtherActiveEmployeeUsesExternalIdentityKeyAsync(employee.Id, externalKey, cancellationToken);
            employee.ExternalIdentityKey = externalKey;
            _employeeRepository.Update(employee);
            await _employeeRepository.SaveChangesAsync(cancellationToken);
        }

        await _gateway.SetRoleIdsForUserAsync(linkedUser.Id, effectiveRoleIds, cancellationToken);
    }

    private async Task<EmployeeLinkedUserSnapshot?> ResolveLinkedUserAsync(Employee employee, CancellationToken cancellationToken)
    {
        if (int.TryParse(employee.ExternalIdentityKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var linkedUserId))
        {
            var byId = await _gateway.GetUserByIdAsync(linkedUserId, cancellationToken);
            if (byId is not null)
                return byId;
        }

        return await _gateway.GetUserByEmailAsync(employee.Email, cancellationToken);
    }

    private async Task EnsureNoOtherActiveEmployeeUsesExternalIdentityKeyAsync(
        int employeeId,
        string externalKey,
        CancellationToken cancellationToken)
    {
        var conflict = await _employeeRepository.GetQueryable()
            .AnyAsync(
                e => !e.IsArchived
                    && e.ExternalIdentityKey == externalKey
                    && e.Id != employeeId,
                cancellationToken);

        if (conflict)
        {
            throw new BusinessRuleException(
                "This user account is already linked to another active employee. Unlink or archive the other employee before linking here.");
        }
    }
}
