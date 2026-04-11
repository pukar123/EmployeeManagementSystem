using EMS.Application.DTOs.Site;

namespace EMS.Application.Services.Sites;

public interface ISiteService
{
    Task<SiteResponseModel> CreateAsync(CreateSiteRequestModel request, CancellationToken cancellationToken = default);

    Task<SiteResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiteResponseModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SiteResponseModel?> UpdateAsync(int id, UpdateSiteRequestModel request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
