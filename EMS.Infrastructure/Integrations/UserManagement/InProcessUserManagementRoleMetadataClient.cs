using EMS.Application.Services.Authorization;
using Pukar.Usermanagement.Application.Services.Internal;

namespace EMS.Infrastructure.Integrations.UserManagement;

/// <summary>
/// In-process replacement for the former HTTP role-metadata client. Reads role metadata
/// directly from the User Management application layer within the same host.
/// </summary>
public sealed class InProcessUserManagementRoleMetadataClient : IUserManagementRoleMetadataClient
{
    private readonly IInternalRoleService _roles;

    public InProcessUserManagementRoleMetadataClient(IInternalRoleService roles)
    {
        _roles = roles;
    }

    public async Task<IReadOnlyList<RoleMetadataSnapshot>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var metadata = await _roles.GetMetadataAsync(cancellationToken);
        return metadata
            .Select(r => new RoleMetadataSnapshot
            {
                Id = r.Id,
                Name = r.Name,
                NormalizedName = r.NormalizedName,
                IsSystem = r.IsSystem,
            })
            .ToList();
    }
}
