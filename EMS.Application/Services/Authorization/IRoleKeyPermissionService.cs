using EMS.Application.DTOs.Authorization;

namespace EMS.Application.Services.Authorization;

public interface IRoleKeyPermissionService
{
    Task<RoleMenuAccessResponseModel> GetAsync(string roleKey, CancellationToken cancellationToken = default);

    Task SetAsync(string roleKey, SetRoleMenuAccessRequestModel request, CancellationToken cancellationToken = default);
}
