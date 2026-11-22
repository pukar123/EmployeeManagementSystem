namespace EMS.Application.Services.Leave;

/// <summary>
/// Decides whether the current user may use another employee's leave data (B06).
/// <list type="bullet">
/// <item><b>View</b> (requests and balances): the employee themselves, a leave admin, or the employee's current
/// direct manager (manager.team permission, current <c>ManagerId</c>, employee not archived).</item>
/// <item><b>Manage</b> (create, update, cancel requests): the employee themselves or a leave admin.</item>
/// <item><b>Attachments</b> (list, upload, download, delete): the employee themselves or a leave admin. Managers are
/// excluded because attachments are mostly health evidence.</item>
/// </list>
/// "Leave admin" is <see cref="ILeaveAdminAccessService"/>. The self-service <c>leave</c> menu grants nothing over
/// other employees. Every denial throws <c>BusinessRuleException</c> with <see cref="LeaveAccessMessages.Denied"/>.
/// </summary>
public interface ILeaveEmployeeAccessService
{
    /// <summary>Allows self, a leave admin or the current direct manager to read the employee's requests and balances.</summary>
    Task EnsureCanViewEmployeeLeaveAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>Allows self or a leave admin to create, update or cancel the employee's leave requests.</summary>
    Task EnsureCanManageEmployeeLeaveAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>Allows self or a leave admin to list, upload, download or delete attachments on the employee's requests.</summary>
    Task EnsureCanAccessLeaveAttachmentsAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the user may act on other employees' leave, which means the user is a leave admin
    /// (<see cref="ILeaveAdminAccessService"/>). Drives the employee portal's <c>canManageOtherEmployeesLeave</c> flag.
    /// </summary>
    Task<bool> CanManageOtherEmployeesLeaveAsync(CancellationToken cancellationToken = default);
}
