using EMS.Application.DTOs.Navigation;

namespace EMS.Application.Services.Menus;

public interface IMenuService
{
    Task<IReadOnlyList<MenuResponseModel>> GetAllFlatAsync(CancellationToken cancellationToken = default);

    Task<MenuResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<MenuResponseModel> CreateAsync(CreateMenuRequestModel request, CancellationToken cancellationToken = default);

    Task<MenuResponseModel?> UpdateAsync(int id, UpdateMenuRequestModel request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
