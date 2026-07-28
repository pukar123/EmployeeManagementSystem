namespace Pukar.Usermanagement.Domain.Repositories.Interface;

public interface IIdempotencyRecordRepository
{
    Task<IdempotencyLookupResult?> TryGetAsync(
        string clientId,
        string route,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IdempotencyClaimResult> TryClaimAsync(
        string clientId,
        string route,
        string idempotencyKey,
        string requestFingerprint,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        long recordId,
        int statusCode,
        string? responseBody,
        string? contentType,
        CancellationToken cancellationToken = default);

    Task DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}

public sealed class IdempotencyLookupResult
{
    public required string RequestFingerprint { get; init; }

    public required int StatusCode { get; init; }

    public string? ResponseBody { get; init; }

    public string? ContentType { get; init; }

    public bool IsComplete { get; init; }

    public long RecordId { get; init; }
}

public sealed class IdempotencyClaimResult
{
    public required bool IsOwner { get; init; }

    public IdempotencyLookupResult? Existing { get; init; }

    public long RecordId { get; init; }
}
