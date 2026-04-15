using System.Diagnostics.Metrics;
using EMS.Application.Services.Authorization;

namespace EMS.API.Services;

public sealed class AuthorizationTelemetry : IAuthorizationTelemetry, IAuthorizationTelemetryReporter
{
    private static readonly Meter Meter = new("EMS.Authorization", "1.0.0");

    private static readonly Counter<long> RoleKeyPathCounter =
        Meter.CreateCounter<long>("ems_auth_rolekey_path_used_total");

    private static readonly Counter<long> LegacyFallbackCounter =
        Meter.CreateCounter<long>("ems_auth_legacy_fallback_used_total");

    private static readonly Counter<long> RoleKeyLegacyMismatchCounter =
        Meter.CreateCounter<long>("ems_auth_rolekey_legacy_mismatch_total");

    private readonly ILogger<AuthorizationTelemetry> _logger;
    private long _roleKeyPathUsedTotal;
    private long _legacyFallbackUsedTotal;
    private long _roleKeyLegacyMismatchTotal;

    public AuthorizationTelemetry(ILogger<AuthorizationTelemetry> logger)
    {
        _logger = logger;
    }

    public void RecordRoleKeyPathUsed(int roleKeyCount, int allowedMenuCount)
    {
        RoleKeyPathCounter.Add(1);
        Interlocked.Increment(ref _roleKeyPathUsedTotal);
        _logger.LogDebug(
            "Authorization role-key path used. roleKeys={RoleKeyCount}, allowedMenus={AllowedMenuCount}",
            roleKeyCount,
            allowedMenuCount);
    }

    public void RecordLegacyFallbackUsed(int userId, int roleKeyCount)
    {
        LegacyFallbackCounter.Add(1);
        Interlocked.Increment(ref _legacyFallbackUsedTotal);
        _logger.LogInformation(
            "Authorization legacy fallback used for user {UserId}. roleKeys={RoleKeyCount}",
            userId,
            roleKeyCount);
    }

    public void RecordRoleKeyLegacyMismatch(
        int userId,
        int roleKeyMenuCount,
        int legacyMenuCount,
        int roleKeyCount)
    {
        RoleKeyLegacyMismatchCounter.Add(1);
        Interlocked.Increment(ref _roleKeyLegacyMismatchTotal);
        _logger.LogWarning(
            "Authorization parity mismatch for user {UserId}. roleKeyMenus={RoleKeyMenuCount}, legacyMenus={LegacyMenuCount}, roleKeys={RoleKeyCount}",
            userId,
            roleKeyMenuCount,
            legacyMenuCount,
            roleKeyCount);
    }

    public AuthorizationTelemetrySnapshot GetSnapshot()
    {
        var roleKeyPathTotal = Interlocked.Read(ref _roleKeyPathUsedTotal);
        var mismatchTotal = Interlocked.Read(ref _roleKeyLegacyMismatchTotal);
        var fallbackTotal = Interlocked.Read(ref _legacyFallbackUsedTotal);
        var mismatchRate = roleKeyPathTotal == 0
            ? 0
            : (double)mismatchTotal / roleKeyPathTotal * 100d;

        return new AuthorizationTelemetrySnapshot
        {
            RoleKeyPathUsedTotal = roleKeyPathTotal,
            LegacyFallbackUsedTotal = fallbackTotal,
            RoleKeyLegacyMismatchTotal = mismatchTotal,
            MismatchRatePercent = Math.Round(mismatchRate, 2),
        };
    }
}
