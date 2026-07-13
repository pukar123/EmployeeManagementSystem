using Pukar.Notifications.Domain.DbModels;

namespace Pukar.Notifications.Domain.Repositories;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Notification?> FindByDedupeKeyAsync(
        int recipientUserId,
        string dedupeKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetInboxAsync(
        int recipientUserId,
        int take,
        int skip,
        bool unreadOnly,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        int recipientUserId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default);

    void Update(Notification notification);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
