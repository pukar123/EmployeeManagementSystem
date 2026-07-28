using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Infrastructure.Repositories;

public sealed class AccountInvitationRepository : BaseRepository<AccountInvitation>, IAccountInvitationRepository
{
    public AccountInvitationRepository(UserManagementDbContext db) : base(db)
    {
    }

    public Task<AccountInvitation?> GetActiveByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
        => Context.AccountInvitations
            .Where(i => i.ExternalCorrelationId == correlationId && i.UsedAtUtc == null && i.RevokedAtUtc == null)
            .OrderByDescending(i => i.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<AccountInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Context.AccountInvitations.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<AccountInvitation>> ListByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
        => await Context.AccountInvitations
            .AsNoTracking()
            .Where(i => i.ExternalCorrelationId == correlationId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task RevokePendingByCorrelationIdAsync(string correlationId, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        var pending = await Context.AccountInvitations
            .Where(i => i.ExternalCorrelationId == correlationId && i.UsedAtUtc == null && i.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var invitation in pending)
        {
            invitation.RevokedAtUtc = revokedAtUtc;
            Update(invitation);
        }

        if (pending.Count > 0)
            await SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryMarkUsedAsync(
        int invitationId,
        byte[] rowVersion,
        DateTime usedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var invitation = await Context.AccountInvitations
            .FirstOrDefaultAsync(
                i => i.Id == invitationId
                     && i.UsedAtUtc == null
                     && i.RevokedAtUtc == null
                     && i.ExpiresAtUtc >= usedAtUtc,
                cancellationToken);

        if (invitation is null)
            return false;

        if (rowVersion.Length > 0 && !invitation.RowVersion.SequenceEqual(rowVersion))
            throw new Pukar.Shared.ConcurrencyConflictException("Invitation was modified concurrently. Please retry.");

        invitation.UsedAtUtc = usedAtUtc;
        try
        {
            await SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new Pukar.Shared.ConcurrencyConflictException("Invitation was modified concurrently. Please retry.");
        }
    }
}
