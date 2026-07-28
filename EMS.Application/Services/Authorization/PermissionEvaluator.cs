using EMS.Application.Services.Authorization;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Authorization;

public sealed class PermissionEvaluator : IPermissionEvaluator
{
    private static readonly IReadOnlyDictionary<string, string[]> RoleKeyAliases = new Dictionary<string, string[]>(
        StringComparer.Ordinal)
    {
        ["ADMIN"] = ["ADMINISTRATOR"],
        ["ADMINISTRATOR"] = ["ADMIN"],
    };

    private readonly IBaseRepository<RoleKeyPermission> _roleKeyPermissions;
    private readonly IBaseRepository<RoleKeyCapability> _roleKeyCapabilities;

    public PermissionEvaluator(
        IBaseRepository<RoleKeyPermission> roleKeyPermissions,
        IBaseRepository<RoleKeyCapability> roleKeyCapabilities)
    {
        _roleKeyPermissions = roleKeyPermissions;
        _roleKeyCapabilities = roleKeyCapabilities;
    }

    public async Task<IReadOnlySet<int>> GetAllowedMenuIdsAsync(
        IReadOnlyList<string> roleKeys,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoleKeys(roleKeys);
        if (normalized.Count == 0)
            return new HashSet<int>();

        var menuIds = await _roleKeyPermissions.GetQueryable()
            .AsNoTracking()
            .Where(rp => normalized.Contains(rp.RoleKey) && rp.Allowed)
            .Select(rp => rp.MenuId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return menuIds.ToHashSet();
    }

    public async Task<bool> IsMenuAllowedAsync(
        IReadOnlyList<string> roleKeys,
        int menuId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoleKeys(roleKeys);
        if (normalized.Count == 0)
            return false;

        return await _roleKeyPermissions.GetQueryable()
            .AsNoTracking()
            .AnyAsync(rp => normalized.Contains(rp.RoleKey) && rp.MenuId == menuId && rp.Allowed, cancellationToken);
    }

    public async Task<bool> HasCapabilityAsync(
        IReadOnlyList<string> roleKeys,
        string capabilityKey,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoleKeys(roleKeys);
        if (normalized.Count == 0 || string.IsNullOrWhiteSpace(capabilityKey))
            return false;

        var keysToCheck = ResolveCapabilityKeysToCheck(capabilityKey.Trim());
        if (keysToCheck.Count == 0)
            return false;

        return await _roleKeyCapabilities.GetQueryable()
            .AsNoTracking()
            .AnyAsync(
                rc => normalized.Contains(rc.RoleKey) && keysToCheck.Contains(rc.CapabilityKey) && rc.Allowed,
                cancellationToken);
    }

    private static IReadOnlyList<string> ResolveCapabilityKeysToCheck(string capabilityKey)
    {
        if (capabilityKey.Equals(EmployeeCapabilities.View, StringComparison.Ordinal))
        {
            return
            [
                EmployeeCapabilities.View,
                ..EmployeeCapabilities.ViewImpliedBy,
            ];
        }

        return [capabilityKey];
    }

    private static IReadOnlyList<string> NormalizeRoleKeys(IReadOnlyList<string> roleKeys)
    {
        var normalized = roleKeys
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Select(static role => role.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var expanded = new HashSet<string>(normalized, StringComparer.Ordinal);
        foreach (var roleKey in normalized)
        {
            if (RoleKeyAliases.TryGetValue(roleKey, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    expanded.Add(alias);
                }
            }
        }

        return expanded.ToList();
    }
}
