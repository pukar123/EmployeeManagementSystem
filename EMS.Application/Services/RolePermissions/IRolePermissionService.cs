using EMS.Application.DTOs.Navigation;

namespace EMS.Application.Services.RolePermissions;

public interface IRolePermissionService
{
    Task<IReadOnlyList<RolePermissionItemModel>> GetForRoleAsync(int roleId, CancellationToken cancellationToken = default);

    Task SetForRoleAsync(int roleId, SetRolePermissionsRequestModel request, CancellationToken cancellationToken = default);
}
