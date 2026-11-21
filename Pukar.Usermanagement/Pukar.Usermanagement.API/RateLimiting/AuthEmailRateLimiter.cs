using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Pukar.Usermanagement.Application.Options;

namespace Pukar.Usermanagement.API.RateLimiting;

/// <summary>
/// Fixed-window limiters keyed by normalised email, one per per-email policy.
/// Registered as a singleton owned by the host, so every host (and every test host) starts with
/// fresh counters. Idle partitions are dropped by the built-in partitioned limiter.
/// </summary>
public sealed class AuthEmailRateLimiter : IDisposable
{
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

    /// <summary>Takes one permit for <paramref name="normalizedEmail"/> under <paramref name="policyName"/>.</summary>
    public RateLimitLease AttemptAcquire(string policyName, string normalizedEmail)
    {
        if (!_limiters.TryGetValue(policyName, out var limiter))
            throw new ArgumentOutOfRangeException(nameof(policyName), policyName, "Unknown per-email rate-limit policy.");

        return limiter.AttemptAcquire(normalizedEmail);
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
        => PartitionedRateLimiter.Create<string, string>(email =>
            RateLimitPartition.GetFixedWindowLimiter(email, _ => ToLimiterOptions(limit)));

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
