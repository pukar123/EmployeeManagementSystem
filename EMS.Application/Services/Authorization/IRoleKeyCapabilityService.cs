using EMS.Application.DTOs.Authorization;

namespace EMS.Application.Services.Authorization;

public interface IRoleKeyCapabilityService
{
    Task<RoleCapabilityAccessResponseModel> GetAsync(string roleKey, CancellationToken cancellationToken = default);

    Task SetAsync(string roleKey, SetRoleCapabilityAccessRequestModel request, CancellationToken cancellationToken = default);
}
