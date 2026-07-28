using Microsoft.EntityFrameworkCore;
using Pukar.Shared;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Infrastructure.Repositories;

public sealed class IdempotencyRecordRepository : IIdempotencyRecordRepository
{
    private readonly UserManagementDbContext _db;

    public IdempotencyRecordRepository(UserManagementDbContext db)
    {
        _db = db;
    }

    public async Task<IdempotencyLookupResult?> TryGetAsync(
        string clientId,
        string route,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ClientId == clientId && x.Route == route && x.IdempotencyKey == idempotencyKey,
                cancellationToken);

        return record is null ? null : ToLookup(record);
    }

    public async Task<IdempotencyClaimResult> TryClaimAsync(
        string clientId,
        string route,
        string idempotencyKey,
        string requestFingerprint,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(
                x => x.ClientId == clientId && x.Route == route && x.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
            {
                throw new ConflictBusinessRuleException(
                    "Idempotency-Key was already used with a different request payload.");
            }

            if (existing.StatusCode == 0)
            {
                return new IdempotencyClaimResult
                {
                    IsOwner = false,
                    Existing = ToLookup(existing),
                    RecordId = existing.Id,
                };
            }

            return new IdempotencyClaimResult
            {
                IsOwner = false,
                Existing = ToLookup(existing),
                RecordId = existing.Id,
            };
        }

        var record = new IdempotencyRecord
        {
            ClientId = clientId,
            Route = route,
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = requestFingerprint,
            StatusCode = 0,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
        };

        _db.IdempotencyRecords.Add(record);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return new IdempotencyClaimResult { IsOwner = true, RecordId = record.Id };
        }
        catch (DbUpdateException)
        {
            var raced = await TryGetAsync(clientId, route, idempotencyKey, cancellationToken)
                ?? throw new ConcurrencyConflictException("Idempotency record conflict. Retry the request.");

            if (!string.Equals(raced.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
            {
                throw new ConflictBusinessRuleException(
                    "Idempotency-Key was already used with a different request payload.");
            }

            return new IdempotencyClaimResult
            {
                IsOwner = false,
                Existing = raced,
                RecordId = raced.RecordId,
            };
        }
    }

    public async Task CompleteAsync(
        long recordId,
        int statusCode,
        string? responseBody,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.IdempotencyRecords.FirstOrDefaultAsync(x => x.Id == recordId, cancellationToken)
            ?? throw new InvalidOperationException("Idempotency record not found.");

        record.StatusCode = statusCode;
        record.ResponseBody = responseBody;
        record.ContentType = contentType;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var expired = await _db.IdempotencyRecords
            .Where(x => x.ExpiresAtUtc < utcNow)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return;

        _db.IdempotencyRecords.RemoveRange(expired);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static IdempotencyLookupResult ToLookup(IdempotencyRecord record)
        => new()
        {
            RecordId = record.Id,
            RequestFingerprint = record.RequestFingerprint,
            StatusCode = record.StatusCode,
            ResponseBody = record.ResponseBody,
            ContentType = record.ContentType,
            IsComplete = record.StatusCode != 0,
        };
}
