using Pukar.Usermanagement.Application.Services.Roles;
using Pukar.Usermanagement.Application.Services.UserRoles;
using Pukar.Usermanagement.Contracts.Roles;
using Pukar.Usermanagement.Contracts.UserRoles;
using Pukar.Usermanagement.Contracts.Users;
using Pukar.Shared;

namespace Pukar.Usermanagement.Application.Services.Internal;

public sealed class InternalRoleService : IInternalRoleService
{
    private readonly IRoleService _roles;
    private readonly IUserRoleService _userRoles;

    public InternalRoleService(IRoleService roles, IUserRoleService userRoles)
    {
        _roles = roles;
        _userRoles = userRoles;
    }

    public Task<IReadOnlyList<RoleMetadataV1ResponseModel>> GetMetadataAsync(CancellationToken cancellationToken = default)
        => _roles.GetMetadataV1Async(cancellationToken);

    public async Task<IReadOnlyList<string>> GetUserRoleKeysAsync(int userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await _userRoles.GetRoleIdsForUserAsync(userId, cancellationToken);
        if (roleIds.Count == 0)
            return Array.Empty<string>();

        var allRoles = await _roles.GetAllAsync(cancellationToken);
        var byId = allRoles.ToDictionary(r => r.Id);
        return roleIds
            .Where(id => byId.ContainsKey(id))
            .Select(id => byId[id].NormalizedName)
            .OrderBy(k => k)
            .ToList();
    }

    public async Task ReplaceUserRolesByKeysAsync(
        int userId,
        IReadOnlyList<string> normalizedRoleKeys,
        CancellationToken cancellationToken = default)
    {
        var allRoles = await _roles.GetAllAsync(cancellationToken);
        var keySet = normalizedRoleKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToUpperInvariant())
            .Distinct()
            .ToHashSet();

        var roleIds = allRoles
            .Where(r => keySet.Contains(r.NormalizedName))
            .Select(r => r.Id)
            .ToList();

        if (keySet.Count != roleIds.Count)
        {
            var missing = keySet.Except(allRoles.Select(r => r.NormalizedName)).ToList();
            throw new BusinessRuleException($"Unknown role keys: {string.Join(", ", missing)}");
        }

        await _userRoles.SetUserRolesAsync(
            userId,
            new AssignUserRolesRequestModel { RoleIds = roleIds },
            cancellationToken);
    }
}
