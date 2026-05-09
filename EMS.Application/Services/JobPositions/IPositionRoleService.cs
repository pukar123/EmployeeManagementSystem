using EMS.Application.DTOs.JobPosition;

namespace EMS.Application.Services.JobPositions;

public interface IPositionRoleService
{
    Task<IReadOnlyList<PositionRoleResponseModel>?> GetByPositionAsync(int jobPositionId, CancellationToken cancellationToken = default);
    Task<bool> SetPositionRolesAsync(int jobPositionId, SetPositionRolesRequestModel request, CancellationToken cancellationToken = default);
}
