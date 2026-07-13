using Pukar.Notifications.Domain.Enums;

namespace Pukar.Notifications.Application.DTOs;

public sealed class CreateNotificationRequestModel
{
    public int? RecipientUserId { get; set; }

    public string? RecipientKey { get; set; }

    public string TypeKey { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? ActionUrl { get; set; }

    public string? MetadataJson { get; set; }

    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Normal;

    public DateTime? ExpiresAtUtc { get; set; }

    public string? DedupeKey { get; set; }
}
