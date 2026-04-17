using System.Text.Json;
using EMS.Application.Services.Authorization;
using EMS.Domain.Database;
using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EMS.Infrastructure.Persistence.Auditing;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> ExcludedEntityNames = new(StringComparer.Ordinal)
    {
        nameof(AuditTrailEntry),
    };

    private readonly IAuditContextAccessor _auditContextAccessor;

    public AuditSaveChangesInterceptor(IAuditContextAccessor auditContextAccessor)
    {
        _auditContextAccessor = auditContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var db = eventData.Context as AppDbContext;
        if (db is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var identity = _auditContextAccessor.GetCurrentUser();
        var correlationId = _auditContextAccessor.GetCorrelationId();
        var now = DateTime.UtcNow;

        var audits = db.ChangeTracker.Entries()
            .Where(IsTrackableEntry)
            .Select(entry => BuildAuditRow(entry, identity, correlationId, now))
            .Where(row => row is not null)
            .Cast<AuditTrailEntry>()
            .ToList();

        if (audits.Count > 0)
        {
            db.AuditTrailEntries.AddRange(audits);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static bool IsTrackableEntry(EntityEntry entry)
    {
        if (entry.Entity is null)
            return false;

        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            return false;

        var entityName = entry.Metadata.ClrType.Name;
        if (ExcludedEntityNames.Contains(entityName))
            return false;

        return true;
    }

    private static AuditTrailEntry? BuildAuditRow(
        EntityEntry entry,
        UserIdentitySnapshot identity,
        string? correlationId,
        DateTime timestampUtc)
    {
        var action = entry.State switch
        {
            EntityState.Added => "Added",
            EntityState.Modified => "Modified",
            EntityState.Deleted => "Deleted",
            _ => null,
        };

        if (action is null)
            return null;

        var entityName = entry.Metadata.ClrType.Name;
        var keyNames = entry.Metadata.FindPrimaryKey()?.Properties.Select(p => p.Name).ToList() ?? [];
        var keyValues = keyNames.ToDictionary(
            k => k,
            k => entry.Property(k).CurrentValue ?? entry.Property(k).OriginalValue ?? "null");

        var oldValues = entry.State switch
        {
            EntityState.Modified => GetModifiedOriginalValues(entry),
            EntityState.Deleted => GetAllOriginalValues(entry),
            _ => null,
        };

        var newValues = entry.State switch
        {
            EntityState.Added => GetAllCurrentValues(entry),
            EntityState.Modified => GetModifiedCurrentValues(entry),
            _ => null,
        };

        return new AuditTrailEntry
        {
            EntityName = entityName,
            EntityKey = JsonSerializer.Serialize(keyValues),
            Action = action,
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues),
            ChangedAtUtc = timestampUtc,
            ActorUserId = identity.UserId,
            ActorEmail = identity.Email,
            ActorUserName = identity.UserName,
            CorrelationId = correlationId,
        };
    }

    private static Dictionary<string, object?> GetAllOriginalValues(EntityEntry entry)
        => entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);

    private static Dictionary<string, object?> GetAllCurrentValues(EntityEntry entry)
        => entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

    private static Dictionary<string, object?> GetModifiedOriginalValues(EntityEntry entry)
        => entry.Properties
            .Where(p => p.IsModified)
            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);

    private static Dictionary<string, object?> GetModifiedCurrentValues(EntityEntry entry)
        => entry.Properties
            .Where(p => p.IsModified)
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
}
