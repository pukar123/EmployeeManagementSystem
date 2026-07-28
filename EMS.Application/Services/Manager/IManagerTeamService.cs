using EMS.Application.DTOs.Manager;

namespace EMS.Application.Services.Manager;

public interface IManagerTeamService
{
    Task<ManagerTeamDashboardResponseModel> GetDashboardAsync(
        ManagerTeamQueryModel query,
        CancellationToken cancellationToken = default);
}
