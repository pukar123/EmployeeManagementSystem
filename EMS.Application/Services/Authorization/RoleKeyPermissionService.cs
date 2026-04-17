using EMS.Application.DTOs.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Authorization;

public sealed class RoleKeyPermissionService : IRoleKeyPermissionService
{
    private const string AdminRoleKey = "ADMIN";

    private readonly IBaseRepository<RoleKeyPermission> _roleKeyPermissions;
    private readonly IBaseRepository<Menu> _menus;

    public RoleKeyPermissionService(
        IBaseRepository<RoleKeyPermission> roleKeyPermissions,
        IBaseRepository<Menu> menus)
    {
        _roleKeyPermissions = roleKeyPermissions;
        _menus = menus;
    }

    public async Task<RoleMenuAccessResponseModel> GetAsync(string roleKey, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoleKey(roleKey);
        var ids = await _roleKeyPermissions.GetQueryable()
            .AsNoTracking()
            .Where(r => r.RoleKey == normalized && r.Allowed)
            .Select(r => r.MenuId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return new RoleMenuAccessResponseModel
        {
            RoleKey = normalized,
            MenuIds = ids,
        };
    }

    public async Task SetAsync(string roleKey, SetRoleMenuAccessRequestModel request, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoleKey(roleKey);
        var distinctIds = request.MenuIds?.Distinct().ToList() ?? new List<int>();

        if (normalized == AdminRoleKey && distinctIds.Count == 0)
            throw new BusinessRuleException("Cannot remove all menu access for the ADMIN role.");

        if (distinctIds.Count > 0)
        {
            var validCount = await _menus.GetQueryable()
                .AsNoTracking()
                .CountAsync(m => distinctIds.Contains(m.Id), cancellationToken);
            if (validCount != distinctIds.Count)
                throw new BusinessRuleException("One or more menu ids are invalid.");
        }

        await using var tx = await _roleKeyPermissions.BeginTransactionAsync(cancellationToken);
        var existing = await _roleKeyPermissions.GetQueryable()
            .Where(r => r.RoleKey == normalized)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
            _roleKeyPermissions.RemoveRange(existing);

        foreach (var menuId in distinctIds)
        {
            await _roleKeyPermissions.AddAsync(
                new RoleKeyPermission
                {
                    RoleKey = normalized,
                    MenuId = menuId,
                    Allowed = true,
                },
                cancellationToken);
        }

        await _roleKeyPermissions.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private static string NormalizeRoleKey(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
            throw new BusinessRuleException("Role key is required.");
        return roleKey.Trim().ToUpperInvariant();
    }
}
