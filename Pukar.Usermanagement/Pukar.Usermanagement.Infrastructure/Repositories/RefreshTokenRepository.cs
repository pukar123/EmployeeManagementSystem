using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(UserManagementDbContext context)
        : base(context)
    {
    }

    public async Task<RefreshToken?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await Context.Set<RefreshToken>()
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash && x.RevokedAtUtc == null && x.ExpiresAtUtc > now,
                cancellationToken);
    }

    public async Task RevokeAllForUserAsync(int userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        var active = await Context.Set<RefreshToken>()
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var t in active)
            t.RevokedAtUtc = revokedAtUtc;

        await Context.SaveChangesAsync(cancellationToken);
    }
}
