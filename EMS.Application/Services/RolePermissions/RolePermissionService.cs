using EMS.Application.DTOs.Navigation;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.RolePermissions;

public sealed class RolePermissionService : IRolePermissionService
{
    private readonly IBaseRepository<RolePermission> _rolePermissions;
    private readonly IBaseRepository<Menu> _menus;

    public RolePermissionService(IBaseRepository<RolePermission> rolePermissions, IBaseRepository<Menu> menus)
    {
        _rolePermissions = rolePermissions;
        _menus = menus;
    }

    public async Task<IReadOnlyList<RolePermissionItemModel>> GetForRoleAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var list = await _rolePermissions.GetQueryable()
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => new RolePermissionItemModel { MenuId = rp.MenuId, Allowed = rp.Allowed })
            .ToListAsync(cancellationToken);
        return list;
    }

    public async Task SetForRoleAsync(int roleId, SetRolePermissionsRequestModel request, CancellationToken cancellationToken = default)
    {
        var items = request.Permissions ?? Array.Empty<RolePermissionItemModel>();
        var menuIds = items.Select(p => p.MenuId).Distinct().ToList();
        foreach (var menuId in menuIds)
        {
            if (await _menus.GetByIdAsync(menuId, cancellationToken) is null)
                throw new BusinessRuleException($"Menu id {menuId} was not found.");
        }

        var existing = await _rolePermissions.GetQueryable()
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);
        _rolePermissions.RemoveRange(existing);
        await _rolePermissions.SaveChangesAsync(cancellationToken);

        foreach (var p in items.GroupBy(x => x.MenuId).Select(g => g.Last()))
        {
            await _rolePermissions.AddAsync(new RolePermission
            {
                RoleId = roleId,
                MenuId = p.MenuId,
                Allowed = p.Allowed,
            }, cancellationToken);
        }

        await _rolePermissions.SaveChangesAsync(cancellationToken);
    }
}
