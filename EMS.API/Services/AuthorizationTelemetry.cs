using System.Diagnostics.Metrics;
using EMS.Application.Services.Authorization;

namespace EMS.API.Services;

public sealed class AuthorizationTelemetry : IAuthorizationTelemetry
{
    private static readonly Meter Meter = new("EMS.Authorization", "1.0.0");

    private static readonly Counter<long> RoleKeyPathCounter =
        Meter.CreateCounter<long>("ems_auth_rolekey_path_used_total");

    private static readonly Counter<long> LegacyFallbackCounter =
        Meter.CreateCounter<long>("ems_auth_legacy_fallback_used_total");

    private static readonly Counter<long> RoleKeyLegacyMismatchCounter =
        Meter.CreateCounter<long>("ems_auth_rolekey_legacy_mismatch_total");

    private readonly ILogger<AuthorizationTelemetry> _logger;

    public AuthorizationTelemetry(ILogger<AuthorizationTelemetry> logger)
    {
        _logger = logger;
    }

    public void RecordRoleKeyPathUsed(int roleKeyCount, int allowedMenuCount)
    {
        RoleKeyPathCounter.Add(1);
        _logger.LogDebug(
            "Authorization role-key path used. roleKeys={RoleKeyCount}, allowedMenus={AllowedMenuCount}",
            roleKeyCount,
            allowedMenuCount);
    }

    public void RecordLegacyFallbackUsed(int userId, int roleKeyCount)
    {
        LegacyFallbackCounter.Add(1);
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
        _logger.LogWarning(
            "Authorization parity mismatch for user {UserId}. roleKeyMenus={RoleKeyMenuCount}, legacyMenus={LegacyMenuCount}, roleKeys={RoleKeyCount}",
            userId,
            roleKeyMenuCount,
            legacyMenuCount,
            roleKeyCount);
    }
}
