namespace EMS.Application.Services.Authorization;

public interface IUserManagementRoleMetadataClient
{
    Task<IReadOnlyList<RoleMetadataSnapshot>> GetRolesAsync(CancellationToken cancellationToken = default);
}
