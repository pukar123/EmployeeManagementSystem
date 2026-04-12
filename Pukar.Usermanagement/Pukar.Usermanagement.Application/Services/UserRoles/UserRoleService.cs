using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Application.DTOs.Roles;
using Pukar.Usermanagement.Application.DTOs.UserRoles;
using Pukar.Usermanagement.Domain.Repositories.Interface;
using Pukar.Shared;

namespace Pukar.Usermanagement.Application.Services.UserRoles;

public sealed class UserRoleService : IUserRoleService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUserRoleRepository _userRoles;

    public UserRoleService(IUserRepository users, IRoleRepository roles, IUserRoleRepository userRoles)
    {
        _users = users;
        _roles = roles;
        _userRoles = userRoles;
    }

    public Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        _userRoles.GetRoleIdsForUserAsync(userId, cancellationToken);

    public async Task<IReadOnlyList<RoleResponseModel>> GetRolesForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (await _users.GetByIdAsync(userId, cancellationToken) is null)
            return Array.Empty<RoleResponseModel>();

        var roleIds = await _userRoles.GetRoleIdsForUserAsync(userId, cancellationToken);
        if (roleIds.Count == 0)
            return Array.Empty<RoleResponseModel>();

        var roles = await _roles.GetQueryable()
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .Select(r => new RoleResponseModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
            })
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task SetUserRolesAsync(int userId, AssignUserRolesRequestModel request, CancellationToken cancellationToken = default)
    {
        if (await _users.GetByIdAsync(userId, cancellationToken) is null)
            throw new BusinessRuleException("User was not found.");

        var roleIds = request.RoleIds?.Distinct().ToList() ?? new List<int>();
        if (roleIds.Count == 0)
        {
            await _userRoles.ReplaceRolesForUserAsync(userId, Array.Empty<int>(), cancellationToken);
            return;
        }

        foreach (var roleId in roleIds)
        {
            if (await _roles.GetByIdAsync(roleId, cancellationToken) is null)
                throw new BusinessRuleException($"Role id {roleId} was not found.");
        }

        await _userRoles.ReplaceRolesForUserAsync(userId, roleIds, cancellationToken);
    }
}
