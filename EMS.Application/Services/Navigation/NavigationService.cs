using EMS.Application.DTOs.Navigation;
using EMS.Application.Services.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMS.Application.Services.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly IIdentityContext _identityContext;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IAuthorizationTelemetry _telemetry;
    private readonly IBaseRepository<Menu> _menus;
    private readonly ILogger<NavigationService> _logger;

    public NavigationService(
        IIdentityContext identityContext,
        IPermissionEvaluator permissionEvaluator,
        IAuthorizationTelemetry telemetry,
        IBaseRepository<Menu> menus,
        ILogger<NavigationService> logger)
    {
        _identityContext = identityContext;
        _permissionEvaluator = permissionEvaluator;
        _telemetry = telemetry;
        _menus = menus;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MenuResponseModel>> GetMenusForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var permittedMenuIds = await GetPermittedMenuIdsFromRoleKeysAsync(userId, cancellationToken);

        if (permittedMenuIds.Count == 0)
            return Array.Empty<MenuResponseModel>();

        return await BuildMenuTreeAsync(permittedMenuIds, cancellationToken);
    }

    private async Task<IReadOnlyCollection<int>> GetPermittedMenuIdsFromRoleKeysAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var roleKeys = _identityContext.GetCurrent().RoleKeys;
        if (roleKeys.Count == 0)
        {
            _logger.LogWarning(
                "No role keys resolved for user {UserId}; returning empty navigation permissions.",
                userId);
        }

        var roleKeyMenuIds = await _permissionEvaluator.GetAllowedMenuIdsAsync(roleKeys, cancellationToken);
        if (roleKeyMenuIds.Count == 0)
        {
            _logger.LogWarning(
                "No allowed menu ids resolved for user {UserId}. RoleKeys={RoleKeys}",
                userId,
                string.Join(",", roleKeys));
        }

        _telemetry.RecordRoleKeyPathUsed(roleKeys.Count, roleKeyMenuIds.Count);
        return roleKeyMenuIds;
    }

    private async Task<IReadOnlyList<MenuResponseModel>> BuildMenuTreeAsync(
        IReadOnlyCollection<int> permittedMenuIds,
        CancellationToken cancellationToken)
    {
        var allMenus = await _menus.GetQueryable().AsNoTracking().ToListAsync(cancellationToken);
        var menuById = allMenus.ToDictionary(m => m.Id);

        var required = new HashSet<int>(permittedMenuIds);
        foreach (var menuId in permittedMenuIds)
        {
            menuById.TryGetValue(menuId, out var current);
            while (current?.ParentMenuId is int parentId)
            {
                required.Add(parentId);
                menuById.TryGetValue(parentId, out current);
            }
        }

        var filtered = allMenus.Where(m => required.Contains(m.Id))
            .OrderBy(static m => m.SortOrder)
            .ThenBy(static m => m.Label)
            .ToList();

        var deduped = DeduplicateByEffectiveRoute(filtered);
        return BuildTree(deduped, null);
    }

    private static IReadOnlyList<Menu> DeduplicateByEffectiveRoute(IReadOnlyList<Menu> menus)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduped = new List<Menu>(menus.Count);

        foreach (var menu in menus)
        {
            var route = menu.RoutePath.Trim();
            if (route.Length > 1)
            {
                route = route.TrimEnd('/');
            }

            var key = $"{menu.ParentMenuId?.ToString() ?? "root"}|{route}|{menu.Label.Trim()}";
            if (seen.Add(key))
            {
                deduped.Add(menu);
            }
        }

        return deduped;
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
