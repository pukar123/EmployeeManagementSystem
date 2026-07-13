using Pukar.Notifications.Domain.DbModels;
using Pukar.Notifications.Domain.Repositories;

namespace EMS.Application.UnitTests.Notifications;

internal sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly List<Notification> _items = [];
    private int _nextId = 1;

    public IReadOnlyList<Notification> Items => _items;

    public void Seed(params Notification[] notifications)
    {
        foreach (var notification in notifications)
        {
            if (notification.Id == 0)
                notification.Id = _nextId++;
            _items.Add(notification);
        }
    }

    public Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task<Notification?> FindByDedupeKeyAsync(
        int recipientUserId,
        string dedupeKey,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x =>
            x.RecipientUserId == recipientUserId
            && x.DedupeKey == dedupeKey
            && !x.IsArchived));

    public Task<IReadOnlyList<Notification>> GetInboxAsync(
        int recipientUserId,
        int take,
        int skip,
        bool unreadOnly,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var query = _items
            .Where(x => x.RecipientUserId == recipientUserId
                        && !x.IsArchived
                        && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > utcNow));

        if (unreadOnly)
            query = query.Where(x => !x.IsRead);

        var result = query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult<IReadOnlyList<Notification>>(result);
    }

    public Task<int> GetUnreadCountAsync(
        int recipientUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_items.Count(x =>
            x.RecipientUserId == recipientUserId
            && !x.IsArchived
            && !x.IsRead
            && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > utcNow)));

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        if (notification.Id == 0)
            notification.Id = _nextId++;
        _items.Add(notification);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
    {
        foreach (var notification in notifications)
        {
            if (notification.Id == 0)
                notification.Id = _nextId++;
            _items.Add(notification);
        }

        return Task.CompletedTask;
    }

    public void Update(Notification notification)
    {
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
