using Pukar.Usermanagement.Application.DTOs.Users;

namespace Pukar.Usermanagement.Application.Services.Users;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserSummaryResponseModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserSummaryResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
