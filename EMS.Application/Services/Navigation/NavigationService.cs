using EMS.Application.DTOs.Navigation;
using EMS.Application.Services.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly IUserEffectiveRoleIdsProvider _roleIds;
    private readonly IIdentityContext _identityContext;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IAuthorizationTelemetry _telemetry;
    private readonly IAuthorizationModeResolver _authorizationMode;
    private readonly IBaseRepository<Menu> _menus;
    private readonly IBaseRepository<RolePermission> _rolePermissions;

    public NavigationService(
        IUserEffectiveRoleIdsProvider roleIds,
        IIdentityContext identityContext,
        IPermissionEvaluator permissionEvaluator,
        IAuthorizationTelemetry telemetry,
        IAuthorizationModeResolver authorizationMode,
        IBaseRepository<Menu> menus,
        IBaseRepository<RolePermission> rolePermissions)
    {
        _roleIds = roleIds;
        _identityContext = identityContext;
        _permissionEvaluator = permissionEvaluator;
        _telemetry = telemetry;
        _authorizationMode = authorizationMode;
        _menus = menus;
        _rolePermissions = rolePermissions;
    }

    public async Task<IReadOnlyList<MenuResponseModel>> GetMenusForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var permittedMenuIds = _authorizationMode.UseRoleKeyMapping
            ? await GetPermittedMenuIdsFromRoleKeysAsync(userId, cancellationToken)
            : await GetPermittedMenuIdsFromLegacyRoleIdsAsync(userId, cancellationToken);

        if (permittedMenuIds.Count == 0)
            return Array.Empty<MenuResponseModel>();

        return await BuildMenuTreeAsync(permittedMenuIds, cancellationToken);
    }

    private async Task<IReadOnlyCollection<int>> GetPermittedMenuIdsFromRoleKeysAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var roleKeys = _identityContext.GetCurrent().RoleKeys;
        var roleKeyMenuIds = await _permissionEvaluator.GetAllowedMenuIdsAsync(roleKeys, cancellationToken);
        _telemetry.RecordRoleKeyPathUsed(roleKeys.Count, roleKeyMenuIds.Count);

        if (!_authorizationMode.EnableLegacyRoleIdFallback)
            return roleKeyMenuIds;

        var legacyMenuIds = await GetPermittedMenuIdsFromLegacyRoleIdsAsync(userId, cancellationToken);

        if (roleKeyMenuIds.Count == 0 && legacyMenuIds.Count > 0)
            _telemetry.RecordLegacyFallbackUsed(userId, roleKeys.Count);

        if (roleKeyMenuIds.Count != legacyMenuIds.Count)
            _telemetry.RecordRoleKeyLegacyMismatch(userId, roleKeyMenuIds.Count, legacyMenuIds.Count, roleKeys.Count);

        if (roleKeyMenuIds.Count > 0)
            return roleKeyMenuIds;

        return legacyMenuIds;
    }

    private async Task<IReadOnlyCollection<int>> GetPermittedMenuIdsFromLegacyRoleIdsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var roleIds = await _roleIds.GetRoleIdsForUserAsync(userId, cancellationToken);
        if (roleIds.Count == 0)
            return Array.Empty<int>();

        return await _rolePermissions.GetQueryable()
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.Allowed)
            .Select(rp => rp.MenuId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<MenuResponseModel>> BuildMenuTreeAsync(
        IReadOnlyCollection<int> permittedMenuIds,
        CancellationToken cancellationToken)
    {
        var allMenus = await _menus.GetQueryable().AsNoTracking().ToListAsync(cancellationToken);

        var required = new HashSet<int>(permittedMenuIds);
        foreach (var menuId in permittedMenuIds)
        {
            var current = allMenus.FirstOrDefault(m => m.Id == menuId);
            while (current?.ParentMenuId is int parentId)
            {
                required.Add(parentId);
                current = allMenus.FirstOrDefault(m => m.Id == parentId);
            }
        }

        var filtered = allMenus.Where(m => required.Contains(m.Id))
            .OrderBy(static m => m.SortOrder)
            .ThenBy(static m => m.Label)
            .ToList();

        return BuildTree(filtered, null);
    }

    private static IReadOnlyList<MenuResponseModel> BuildTree(IReadOnlyList<Menu> flat, int? parentId)
    {
        var children = flat
            .Where(m => m.ParentMenuId == parentId)
            .Select(m => new MenuResponseModel
            {
                Id = m.Id,
                Key = m.Key,
                Label = m.Label,
                RoutePath = m.RoutePath,
                ParentMenuId = m.ParentMenuId,
                SortOrder = m.SortOrder,
                IconKey = m.IconKey,
                Children = BuildTree(flat, m.Id),
            })
            .ToList();
        return children;
    }
}
