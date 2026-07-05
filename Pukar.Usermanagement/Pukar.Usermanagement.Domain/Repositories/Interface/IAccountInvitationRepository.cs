using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Repositories.Interface;

public interface IAccountInvitationRepository : IBaseRepository<AccountInvitation>
{
    Task<AccountInvitation?> GetActiveByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    Task<AccountInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountInvitation>> ListByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    Task RevokePendingByCorrelationIdAsync(string correlationId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Atomically marks invitation used when still pending. Returns false if already used/revoked/expired.</summary>
    Task<bool> TryMarkUsedAsync(int invitationId, byte[] rowVersion, DateTime usedAtUtc, CancellationToken cancellationToken = default);
}
