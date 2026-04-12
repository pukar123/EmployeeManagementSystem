using EMS.Application.DTOs.Navigation;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly IUserEffectiveRoleIdsProvider _roleIds;
    private readonly IBaseRepository<Menu> _menus;
    private readonly IBaseRepository<RolePermission> _rolePermissions;

    public NavigationService(
        IUserEffectiveRoleIdsProvider roleIds,
        IBaseRepository<Menu> menus,
        IBaseRepository<RolePermission> rolePermissions)
    {
        _roleIds = roleIds;
        _menus = menus;
        _rolePermissions = rolePermissions;
    }

    public async Task<IReadOnlyList<MenuResponseModel>> GetMenusForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await _roleIds.GetRoleIdsForUserAsync(userId, cancellationToken);
        if (roleIds.Count == 0)
            return Array.Empty<MenuResponseModel>();

        var permittedMenuIds = await _rolePermissions.GetQueryable()
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.Allowed)
            .Select(rp => rp.MenuId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (permittedMenuIds.Count == 0)
            return Array.Empty<MenuResponseModel>();

        var allMenus = await _menus.GetQueryable()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var required = new HashSet<int>(permittedMenuIds);
        foreach (var menuId in permittedMenuIds)
        {
            var current = allMenus.FirstOrDefault(m => m.Id == menuId);
            while (current?.ParentMenuId is int p)
            {
                required.Add(p);
                current = allMenus.FirstOrDefault(m => m.Id == p);
            }
        }

        var filtered = allMenus
            .Where(m => required.Contains(m.Id))
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Label)
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
