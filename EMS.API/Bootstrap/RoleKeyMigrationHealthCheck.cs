using EMS.Application.Services.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EMS.API.Bootstrap;

public sealed class RoleKeyMigrationHealthCheck : IHealthCheck
{
    private readonly ILegacyRoleKeyGuard _guard;

    public RoleKeyMigrationHealthCheck(ILegacyRoleKeyGuard guard)
    {
        _guard = guard;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var status = await _guard.GetStatusAsync(cancellationToken);
        if (!status.HasUnresolvedLegacyKeys)
            return HealthCheckResult.Healthy("All role keys use normalized names.");

        return HealthCheckResult.Unhealthy(
            $"Unresolved legacy RoleKey rows remain (position={status.PositionRoleLegacyCount}, assignment={status.EmployeeAssignmentLegacyCount}). " +
            "Run UmDbSplitMigrator backfill or RoleKeyBackfillHostedService recovery.");
    }
}
