using Pukar.Notifications.Domain.Enums;

namespace Pukar.Notifications.Application.DTOs;

public sealed class NotificationResponseModel
{
    public int Id { get; set; }

    public string TypeKey { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? ActionUrl { get; set; }

    public string? MetadataJson { get; set; }

    public NotificationSeverity Severity { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}
