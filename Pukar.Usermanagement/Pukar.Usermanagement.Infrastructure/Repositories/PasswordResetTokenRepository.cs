using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository : BaseRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(UserManagementDbContext context)
        : base(context)
    {
    }

    public Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
        => Context.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public Task<PasswordResetToken?> GetLatestForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
        => Context.PasswordResetTokens
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task RevokeActiveForUserAsync(
        int userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var active = await Context.PasswordResetTokens
            .Where(x => x.UserId == userId && x.UsedAtUtc == null && x.ExpiresAtUtc > revokedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
            token.UsedAtUtc = revokedAtUtc;
    }
}
