namespace EMS.Application.Services.Manager;

/// <summary>
/// Resolves which manager's direct reports the current user may view on the team dashboard.
/// </summary>
public interface IManagerTeamAccessService
{
    Task<int> ResolveEffectiveManagerIdAsync(
        int organizationId,
        int? managerId,
        CancellationToken cancellationToken = default);

    Task<bool> CanViewTeamDashboardAsync(CancellationToken cancellationToken = default);

    Task<bool> IsDirectReportAsync(
        int managerEmployeeId,
        int employeeId,
        CancellationToken cancellationToken = default);
}

public static class ManagerTeamAccessMessages
{
    public const string Denied = "Manager team access denied.";
}
