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

    public PermissionEvaluator(IBaseRepository<RoleKeyPermission> roleKeyPermissions)
    {
        _roleKeyPermissions = roleKeyPermissions;
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
