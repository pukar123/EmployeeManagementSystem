using Pukar.Usermanagement.Contracts.Users;

namespace Pukar.Usermanagement.Application.Services.Internal;

public interface IInternalUserService
{
    Task<IReadOnlyList<UserSummaryResponseModel>> BatchLookupAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default);

    Task<UserSummaryResponseModel?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ActivateAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int userId, CancellationToken cancellationToken = default);

    Task RevokeAllSessionsAsync(int userId, CancellationToken cancellationToken = default);
}
