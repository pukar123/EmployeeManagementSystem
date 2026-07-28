using Pukar.Notifications.Application.DTOs;
using Pukar.Notifications.Application.Mapping;
using Pukar.Notifications.Domain.DbModels;
using Pukar.Notifications.Domain.Repositories;
using Pukar.Shared;

namespace Pukar.Notifications.Application.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationCurrentUserAccessor _currentUser;

    public NotificationService(
        INotificationRepository notifications,
        INotificationCurrentUserAccessor currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public async Task<NotificationResponseModel> CreateAsync(
        CreateNotificationRequestModel request,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        if (request.RecipientUserId is int userId
            && !string.IsNullOrWhiteSpace(request.DedupeKey))
        {
            var existing = await _notifications.FindByDedupeKeyAsync(
                userId,
                request.DedupeKey.Trim(),
                cancellationToken);
            if (existing is not null)
                return NotificationMapper.ToResponse(existing);
        }

        var now = DateTime.UtcNow;
        var entity = NotificationMapper.ToEntity(request, now);
        await _notifications.AddAsync(entity, cancellationToken);
        await _notifications.SaveChangesAsync(cancellationToken);
        return NotificationMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<NotificationResponseModel>> CreateBulkAsync(
        IReadOnlyList<CreateNotificationRequestModel> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
            return [];

        var results = new List<NotificationResponseModel>(requests.Count);
        foreach (var request in requests)
        {
            var created = await CreateAsync(request, cancellationToken);
            results.Add(created);
        }

        return results;
    }

    public async Task<IReadOnlyList<NotificationResponseModel>> GetForCurrentUserAsync(
        NotificationQueryModel query,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var take = query.Take <= 0 ? 50 : Math.Min(query.Take, 100);
        var skip = query.Skip < 0 ? 0 : query.Skip;

        var rows = await _notifications.GetInboxAsync(
            userId,
            take,
            skip,
            query.UnreadOnly,
            DateTime.UtcNow,
            cancellationToken);

        return rows.Select(NotificationMapper.ToResponse).ToList();
    }

    public async Task<NotificationUnreadCountResponseModel> GetUnreadCountAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var count = await _notifications.GetUnreadCountAsync(userId, DateTime.UtcNow, cancellationToken);
        return new NotificationUnreadCountResponseModel { Count = count };
    }

    public async Task<NotificationResponseModel> MarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var entity = await GetOwnedNotificationAsync(id, userId, cancellationToken);

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            entity.ReadAtUtc = DateTime.UtcNow;
            _notifications.Update(entity);
            await _notifications.SaveChangesAsync(cancellationToken);
        }

        return NotificationMapper.ToResponse(entity);
    }

    public async Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var unread = await _notifications.GetInboxAsync(
            userId,
            take: 500,
            skip: 0,
            unreadOnly: true,
            utcNow: DateTime.UtcNow,
            cancellationToken);

        if (unread.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        foreach (var entity in unread)
        {
            entity.IsRead = true;
            entity.ReadAtUtc = now;
            _notifications.Update(entity);
        }

        await _notifications.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    public async Task ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var entity = await GetOwnedNotificationAsync(id, userId, cancellationToken);
        entity.IsArchived = true;
        _notifications.Update(entity);
        await _notifications.SaveChangesAsync(cancellationToken);
    }

    private async Task<Notification> GetOwnedNotificationAsync(
        int id,
        int userId,
        CancellationToken cancellationToken)
    {
        var entity = await _notifications.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.IsArchived || entity.RecipientUserId != userId)
            throw new BusinessRuleException("Notification was not found.");

        return entity;
    }

    private int RequireCurrentUserId()
    {
        var userId = _currentUser.GetCurrentUserId();
        if (userId is null)
            throw new BusinessRuleException("User is not authenticated.");

        return userId.Value;
    }

    private static void ValidateCreateRequest(CreateNotificationRequestModel request)
    {
        if (request.RecipientUserId is null && string.IsNullOrWhiteSpace(request.RecipientKey))
            throw new BusinessRuleException("Recipient user id or recipient key is required.");

        request.TypeKey = StringHelper.NormalizeRequired(request.TypeKey);
        request.Title = StringHelper.NormalizeRequired(request.Title);
        request.Body = StringHelper.NormalizeRequired(request.Body);
        request.ActionUrl = StringHelper.NormalizeOptional(request.ActionUrl);
        request.MetadataJson = StringHelper.NormalizeOptional(request.MetadataJson);
        request.RecipientKey = StringHelper.NormalizeOptional(request.RecipientKey);
        request.DedupeKey = StringHelper.NormalizeOptional(request.DedupeKey);
    }
}
