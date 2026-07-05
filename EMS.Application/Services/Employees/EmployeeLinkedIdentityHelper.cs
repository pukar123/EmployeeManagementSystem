using System.Globalization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public static class EmployeeLinkedIdentityHelper
{
    public static async Task<EmployeeLinkedUserSnapshot?> TryResolveLinkedUserAsync(
        Employee employee,
        IEmployeeUserManagementGateway gateway,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(employee.ExternalIdentityKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var linkedUserId))
            return null;

        return await gateway.GetUserByIdAsync(linkedUserId, cancellationToken);
    }

    public static async Task EnsureNoOtherActiveEmployeeUsesExternalIdentityKeyAsync(
        IBaseRepository<Employee> employees,
        int employeeId,
        string externalKey,
        CancellationToken cancellationToken)
    {
        var conflict = await employees.GetQueryable()
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

    public static async Task RevokeLinkedIdentityAsync(
        Employee employee,
        IEmployeeUserManagementGateway gateway,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var linkedUser = await TryResolveLinkedUserAsync(employee, gateway, cancellationToken);
        if (linkedUser is null)
            return;

        var keyPrefix = idempotencyKey ?? $"revoke-identity:employee:{employee.Id}";
        await gateway.RevokeOperationalAccessAsync(linkedUser.Id, $"{keyPrefix}:roles", cancellationToken);
        await gateway.DeactivateLinkedUserAsync(linkedUser.Id, $"{keyPrefix}:deactivate", cancellationToken);
        await gateway.RevokeSessionsAsync(linkedUser.Id, $"{keyPrefix}:sessions", cancellationToken);
    }
}
