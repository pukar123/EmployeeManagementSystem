using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Application.DTOs.Roles;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;
using Pukar.Shared;

namespace Pukar.Usermanagement.Application.Services.Roles;

public sealed class RoleService : IRoleService
{
    private readonly IRoleRepository _roles;

    public RoleService(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<IReadOnlyList<RoleResponseModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _roles.GetQueryable()
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<RoleResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _roles.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<RoleResponseModel> CreateAsync(CreateRoleRequestModel request, CancellationToken cancellationToken = default)
    {
        var name = StringHelper.NormalizeRequired(request.Name);
        var normalized = NormalizeRoleName(name);

        if (await _roles.GetQueryable().AnyAsync(r => r.NormalizedName == normalized, cancellationToken))
            throw new BusinessRuleException("A role with this name already exists.");

        var entity = new Role
        {
            Name = name,
            NormalizedName = normalized,
            Description = StringHelper.NormalizeOptional(request.Description),
            IsSystem = false,
        };

        await _roles.AddAsync(entity, cancellationToken);
        await _roles.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<RoleResponseModel?> UpdateAsync(int id, UpdateRoleRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _roles.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        if (entity.IsSystem)
            throw new BusinessRuleException("System roles cannot be modified.");

        var name = StringHelper.NormalizeRequired(request.Name);
        var normalized = NormalizeRoleName(name);

        if (await _roles.GetQueryable().AnyAsync(r => r.Id != id && r.NormalizedName == normalized, cancellationToken))
            throw new BusinessRuleException("A role with this name already exists.");

        entity.Name = name;
        entity.NormalizedName = normalized;
        entity.Description = StringHelper.NormalizeOptional(request.Description);

        _roles.Update(entity);
        await _roles.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _roles.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        if (entity.IsSystem)
            throw new BusinessRuleException("System roles cannot be deleted.");

        _roles.Remove(entity);
        await _roles.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeRoleName(string name) => name.Trim().ToUpperInvariant();

    private static RoleResponseModel Map(Role r) =>
        new()
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsSystem = r.IsSystem,
        };
}
