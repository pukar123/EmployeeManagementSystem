using EMS.Application.DTOs.Navigation;

namespace EMS.Application.Services.Navigation;

public interface INavigationService
{
    Task<IReadOnlyList<MenuResponseModel>> GetMenusForUserAsync(int userId, CancellationToken cancellationToken = default);
}
