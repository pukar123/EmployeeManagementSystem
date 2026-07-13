namespace Pukar.Notifications.Application.Services;

public interface INotificationCurrentUserAccessor
{
    int? GetCurrentUserId();
}
