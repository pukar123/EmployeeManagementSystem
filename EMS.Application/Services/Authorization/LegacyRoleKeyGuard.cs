using EMS.Domain.Database;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Authorization;

public sealed class LegacyRoleKeyStatus
{
    public int PositionRoleLegacyCount { get; init; }

    public int EmployeeAssignmentLegacyCount { get; init; }

    public bool HasUnresolvedLegacyKeys => PositionRoleLegacyCount > 0 || EmployeeAssignmentLegacyCount > 0;
}

public interface ILegacyRoleKeyGuard
{
    Task<LegacyRoleKeyStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    Task EnsureRoleMutationsAllowedAsync(CancellationToken cancellationToken = default);
}

public sealed class LegacyRoleKeyGuard : ILegacyRoleKeyGuard
{
    private readonly AppDbContext _db;

    public LegacyRoleKeyGuard(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LegacyRoleKeyStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var positionCount = await _db.PositionRoles
            .Where(x => x.RoleKey != null && EF.Functions.Like(x.RoleKey, "[0-9]%"))
            .CountAsync(cancellationToken);

        var assignmentCount = await _db.EmployeeRoleAssignments
            .Where(x => x.RoleKey != null && EF.Functions.Like(x.RoleKey, "[0-9]%"))
            .CountAsync(cancellationToken);

        return new LegacyRoleKeyStatus
        {
            PositionRoleLegacyCount = positionCount,
            EmployeeAssignmentLegacyCount = assignmentCount,
        };
    }

    public async Task EnsureRoleMutationsAllowedAsync(CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(cancellationToken);
        if (!status.HasUnresolvedLegacyKeys)
            return;

        throw new Pukar.Shared.BusinessRuleException(
            "Role changes are blocked until legacy numeric RoleKey values are backfilled. " +
            "Run the UmDbSplitMigrator apply/validate sequence or restart EMS after User Management metadata is available.");
    }
}
