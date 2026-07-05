using Pukar.Usermanagement.Contracts.Users;

namespace Pukar.Usermanagement.Application.Services.Users;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserSummaryResponseModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSummaryResponseModel>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);

    Task<UserSummaryResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<UserSummaryResponseModel?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserSummaryResponseModel> CreateAsync(CreateUserRequestModel request, CancellationToken cancellationToken = default);

    Task<UserSummaryResponseModel?> UpdateAsync(int id, UpdateUserRequestModel request, CancellationToken cancellationToken = default);

    Task<bool> AdminSetPasswordAsync(int id, AdminSetPasswordRequestModel request, CancellationToken cancellationToken = default);
}
