using System.Globalization;
using EMS.Application.Services.Authorization;
using EMS.Domain.Database;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.Bootstrap;

/// <summary>
/// One-shot backfill: maps legacy numeric RoleKey values (from RoleId migration) to UM NormalizedName.
/// </summary>
public sealed class RoleKeyBackfillHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<RoleKeyBackfillHostedService> _logger;

    public RoleKeyBackfillHostedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILogger<RoleKeyBackfillHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var metadataClient = scope.ServiceProvider.GetRequiredService<IUserManagementRoleMetadataClient>();
        var guard = scope.ServiceProvider.GetRequiredService<ILegacyRoleKeyGuard>();

        var status = await guard.GetStatusAsync(cancellationToken);
        if (!status.HasUnresolvedLegacyKeys)
            return;

        var positionLegacy = await db.PositionRoles
            .Where(x => x.RoleKey != null && EF.Functions.Like(x.RoleKey, "[0-9]%"))
            .ToListAsync(cancellationToken);
        var assignmentLegacy = await db.EmployeeRoleAssignments
            .Where(x => x.RoleKey != null && EF.Functions.Like(x.RoleKey, "[0-9]%"))
            .ToListAsync(cancellationToken);

        var metadata = await metadataClient.GetRolesAsync(cancellationToken);
        if (metadata.Count == 0)
        {
            var message =
                $"RoleKey backfill cannot complete: User Management role metadata unavailable ({positionLegacy.Count} position, {assignmentLegacy.Count} assignment rows pending).";
            _logger.LogError(message);
            FailStartupIfRequired(message);
            return;
        }

        var byId = metadata.ToDictionary(x => x.Id.ToString(CultureInfo.InvariantCulture), x => x.NormalizedName);

        var updated = 0;
        var orphans = 0;

        foreach (var row in positionLegacy)
        {
            if (byId.TryGetValue(row.RoleKey, out var key))
            {
                row.RoleKey = key;
                updated++;
            }
            else
            {
                orphans++;
                _logger.LogWarning("PositionRole {Id} has unknown legacy RoleId key {RoleKey}.", row.Id, row.RoleKey);
            }
        }

        foreach (var row in assignmentLegacy)
        {
            if (byId.TryGetValue(row.RoleKey, out var key))
            {
                row.RoleKey = key;
                updated++;
            }
            else
            {
                orphans++;
                _logger.LogWarning("EmployeeRoleAssignment {Id} has unknown legacy RoleId key {RoleKey}.", row.Id, row.RoleKey);
            }
        }

        if (updated > 0)
            await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "RoleKey backfill completed. Updated={Updated}, Orphans={Orphans}.",
            updated,
            orphans);

        var remaining = await guard.GetStatusAsync(cancellationToken);
        if (remaining.HasUnresolvedLegacyKeys || orphans > 0)
        {
            var message =
                "RoleKey backfill left unresolved legacy keys. Run tools/UmDbSplitMigrator apply/validate or see docs/ems-um-db-split-cutover.md recovery.";
            _logger.LogError(
                "{Message} Remaining position={PositionCount}, assignment={AssignmentCount}, orphans={Orphans}.",
                message,
                remaining.PositionRoleLegacyCount,
                remaining.EmployeeAssignmentLegacyCount,
                orphans);
            FailStartupIfRequired(message);
        }
    }

    private void FailStartupIfRequired(string message)
    {
        if (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
            return;

        throw new InvalidOperationException(message);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
