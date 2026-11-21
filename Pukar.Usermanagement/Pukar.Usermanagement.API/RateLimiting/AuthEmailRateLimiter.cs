using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Pukar.Usermanagement.Application.Options;

namespace Pukar.Usermanagement.API.RateLimiting;

/// <summary>
/// Fixed-window limiters keyed by email, one per per-email policy.
/// Registered as a singleton owned by the host, so every host (and every test host) starts with
/// fresh counters.
/// <para>
/// Memory is bounded: the key is a fixed-length SHA-256 hex of the normalised email (never the raw
/// text), emails that are blank or longer than <see cref="MaxEmailLength"/> get no counter at all, and
/// the built-in partitioned limiter removes a partition once its window has refilled and it has been
/// idle for a short time. So the store holds at most one 64-character key per distinct real-length
/// email seen within a window, and the number of those is capped by the per-IP limits.
/// </para>
/// </summary>
public sealed class AuthEmailRateLimiter : IDisposable
{
    /// <summary>Longest email that gets a per-email counter (the RFC 5321 path limit is 320 characters).</summary>
    public const int MaxEmailLength = 320;

    private readonly Dictionary<string, PartitionedRateLimiter<string>> _limiters;
    private readonly AuthRateLimitingOptions _options;

    public AuthEmailRateLimiter(IOptions<AuthRateLimitingOptions> options)
    {
        _options = options.Value;
        _limiters = AuthRateLimitPolicies.PerEmailPolicies.ToDictionary(
            policy => policy,
            policy => CreateLimiter(AuthRateLimitPolicies.LimitFor(policy, _options)),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// The counter key for <paramref name="email"/>: SHA-256 hex (upper case, 64 characters) of the
    /// trimmed, lower-cased email. Null when the email is blank or longer than <see cref="MaxEmailLength"/>,
    /// which means "do not count this per email" (the per-IP limit and normal validation still apply,
    /// so the caller gets the same answer it would have got without this limiter).
    /// </summary>
    public static string? PartitionKeyFor(string? email)
    {
        if (email is null)
            return null;

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length == 0 || normalized.Length > MaxEmailLength)
            return null;

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    /// <summary>Takes one permit for the counter <paramref name="partitionKey"/> (from <see cref="PartitionKeyFor"/>).</summary>
    public RateLimitLease AttemptAcquire(string policyName, string partitionKey)
    {
        if (!_limiters.TryGetValue(policyName, out var limiter))
            throw new ArgumentOutOfRangeException(nameof(policyName), policyName, "Unknown per-email rate-limit policy.");

        return limiter.AttemptAcquire(partitionKey);
    }

    /// <summary>The policy's window, used for Retry-After when the lease has no retry metadata.</summary>
    public TimeSpan WindowFor(string policyName)
        => AuthRateLimitPolicies.LimitFor(policyName, _options).Window;

    public void Dispose()
    {
        foreach (var limiter in _limiters.Values)
            limiter.Dispose();
    }

    private static PartitionedRateLimiter<string> CreateLimiter(FixedWindowLimitOptions limit)
        => PartitionedRateLimiter.Create<string, string>(partitionKey =>
            RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => ToLimiterOptions(limit)));

    internal static FixedWindowRateLimiterOptions ToLimiterOptions(FixedWindowLimitOptions limit)
        => new()
        {
            PermitLimit = limit.PermitLimit,
            Window = limit.Window,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        };
}
