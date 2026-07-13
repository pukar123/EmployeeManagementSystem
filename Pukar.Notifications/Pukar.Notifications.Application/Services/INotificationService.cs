using Pukar.Notifications.Application.DTOs;

namespace Pukar.Notifications.Application.Services;

public interface INotificationService
{
    Task<NotificationResponseModel> CreateAsync(
        CreateNotificationRequestModel request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationResponseModel>> CreateBulkAsync(
        IReadOnlyList<CreateNotificationRequestModel> requests,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationResponseModel>> GetForCurrentUserAsync(
        NotificationQueryModel query,
        CancellationToken cancellationToken = default);

    Task<NotificationUnreadCountResponseModel> GetUnreadCountAsync(
        CancellationToken cancellationToken = default);

    Task<NotificationResponseModel> MarkReadAsync(int id, CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default);

    Task ArchiveAsync(int id, CancellationToken cancellationToken = default);
}
