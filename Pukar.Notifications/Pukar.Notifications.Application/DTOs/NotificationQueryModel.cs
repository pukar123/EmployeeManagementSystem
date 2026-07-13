namespace Pukar.Notifications.Application.DTOs;

public sealed class NotificationQueryModel
{
    public int Take { get; set; } = 50;

    public int Skip { get; set; }

    public bool UnreadOnly { get; set; }
}
