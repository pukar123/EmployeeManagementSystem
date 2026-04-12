using EMS.Application.DTOs.Navigation;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Menus;

public sealed class MenuService : IMenuService
{
    private readonly IBaseRepository<Menu> _menus;

    public MenuService(IBaseRepository<Menu> menus)
    {
        _menus = menus;
    }

    public async Task<IReadOnlyList<MenuResponseModel>> GetAllFlatAsync(CancellationToken cancellationToken = default)
    {
        var list = await _menus.GetQueryable()
            .AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Label)
            .ToListAsync(cancellationToken);
        return list.Select(MapFlat).ToList();
    }

    public async Task<MenuResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _menus.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapFlat(entity);
    }

    public async Task<MenuResponseModel> CreateAsync(CreateMenuRequestModel request, CancellationToken cancellationToken = default)
    {
        var key = StringHelper.NormalizeRequired(request.Key);
        if (await _menus.GetQueryable().AnyAsync(m => m.Key == key, cancellationToken))
            throw new BusinessRuleException("A menu with this key already exists.");

        if (request.ParentMenuId is int pid)
        {
            if (await _menus.GetByIdAsync(pid, cancellationToken) is null)
                throw new BusinessRuleException("Parent menu was not found.");
        }

        var entity = new Menu
        {
            Key = key,
            Label = StringHelper.NormalizeRequired(request.Label),
            RoutePath = StringHelper.NormalizeRequired(request.RoutePath),
            ParentMenuId = request.ParentMenuId,
            SortOrder = request.SortOrder,
            IconKey = StringHelper.NormalizeOptional(request.IconKey),
        };

        await _menus.AddAsync(entity, cancellationToken);
        await _menus.SaveChangesAsync(cancellationToken);

        return MapFlat(entity);
    }

    public async Task<MenuResponseModel?> UpdateAsync(int id, UpdateMenuRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _menus.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        var key = StringHelper.NormalizeRequired(request.Key);
        if (await _menus.GetQueryable().AnyAsync(m => m.Id != id && m.Key == key, cancellationToken))
            throw new BusinessRuleException("A menu with this key already exists.");

        if (request.ParentMenuId is int pid)
        {
            if (pid == id)
                throw new BusinessRuleException("Menu cannot be its own parent.");
            if (await _menus.GetByIdAsync(pid, cancellationToken) is null)
                throw new BusinessRuleException("Parent menu was not found.");
        }

        entity.Key = key;
        entity.Label = StringHelper.NormalizeRequired(request.Label);
        entity.RoutePath = StringHelper.NormalizeRequired(request.RoutePath);
        entity.ParentMenuId = request.ParentMenuId;
        entity.SortOrder = request.SortOrder;
        entity.IconKey = StringHelper.NormalizeOptional(request.IconKey);

        _menus.Update(entity);
        await _menus.SaveChangesAsync(cancellationToken);

        return MapFlat(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _menus.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        if (await _menus.GetQueryable().AnyAsync(m => m.ParentMenuId == id, cancellationToken))
            throw new BusinessRuleException("Cannot delete a menu that has child menus.");

        _menus.Remove(entity);
        await _menus.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static MenuResponseModel MapFlat(Menu m) =>
        new()
        {
            Id = m.Id,
            Key = m.Key,
            Label = m.Label,
            RoutePath = m.RoutePath,
            ParentMenuId = m.ParentMenuId,
            SortOrder = m.SortOrder,
            IconKey = m.IconKey,
            Children = Array.Empty<MenuResponseModel>(),
        };
}
