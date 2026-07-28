using Pukar.Notifications.Application.DTOs;
using Pukar.Notifications.Domain.DbModels;

namespace Pukar.Notifications.Application.Mapping;

internal static class NotificationMapper
{
    public static NotificationResponseModel ToResponse(Notification entity)
        => new()
        {
            Id = entity.Id,
            TypeKey = entity.TypeKey,
            Title = entity.Title,
            Body = entity.Body,
            ActionUrl = entity.ActionUrl,
            MetadataJson = entity.MetadataJson,
            Severity = entity.Severity,
            IsRead = entity.IsRead,
            CreatedAtUtc = entity.CreatedAtUtc,
            ReadAtUtc = entity.ReadAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
        };

    public static Notification ToEntity(CreateNotificationRequestModel request, DateTime createdAtUtc)
        => new()
        {
            RecipientUserId = request.RecipientUserId,
            RecipientKey = request.RecipientKey,
            TypeKey = request.TypeKey,
            Title = request.Title,
            Body = request.Body,
            ActionUrl = request.ActionUrl,
            MetadataJson = request.MetadataJson,
            Severity = request.Severity,
            ExpiresAtUtc = request.ExpiresAtUtc,
            DedupeKey = request.DedupeKey,
            IsRead = false,
            IsArchived = false,
            CreatedAtUtc = createdAtUtc,
        };
}
