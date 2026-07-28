using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Repositories.Interface;

public interface IPasswordResetTokenRepository : IBaseRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<PasswordResetToken?> GetLatestForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task RevokeActiveForUserAsync(int userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}
