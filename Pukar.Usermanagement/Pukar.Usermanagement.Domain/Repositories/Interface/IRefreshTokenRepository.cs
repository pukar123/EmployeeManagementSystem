using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Repositories.Interface;

public interface IRefreshTokenRepository : IBaseRepository<RefreshToken>
{
    Task<RefreshToken?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(int userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}
