using EMS.Domain.Database;
using Microsoft.EntityFrameworkCore;
using Pukar.Notifications.Domain.DbModels;
using Pukar.Notifications.Domain.Repositories;

namespace EMS.Infrastructure.Repositories.Implementations;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Notifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Notification?> FindByDedupeKeyAsync(
        int recipientUserId,
        string dedupeKey,
        CancellationToken cancellationToken = default)
        => _context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.RecipientUserId == recipientUserId
                     && x.DedupeKey == dedupeKey
                     && !x.IsArchived,
                cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetInboxAsync(
        int recipientUserId,
        int take,
        int skip,
        bool unreadOnly,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(x => x.RecipientUserId == recipientUserId
                        && !x.IsArchived
                        && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > utcNow));

        if (unreadOnly)
            query = query.Where(x => !x.IsRead);

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetUnreadCountAsync(
        int recipientUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
        => _context.Notifications
            .AsNoTracking()
            .CountAsync(
                x => x.RecipientUserId == recipientUserId
                     && !x.IsArchived
                     && !x.IsRead
                     && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > utcNow),
                cancellationToken);

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => _context.Notifications.AddAsync(notification, cancellationToken).AsTask();

    public Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
        => _context.Notifications.AddRangeAsync(notifications, cancellationToken);

    public void Update(Notification notification)
        => _context.Notifications.Update(notification);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
