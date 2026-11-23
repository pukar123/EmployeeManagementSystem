namespace EMS.Application.Services.Attendance;

/// <summary>
/// Decides whose attendance the current user may record and read (B07).
/// <list type="bullet">
/// <item><b>Actions</b> (check-in, check-out, start and end break): the caller's own linked employee, or any
/// employee for an attendance admin. Another employee's id from anyone else is refused, not ignored.</item>
/// <item><b>Manual entries</b>: attendance admins only, even for the caller's own record.</item>
/// <item><b>Per-employee reads</b> (records, summary): the employee themselves, an attendance admin, or the
/// employee's current direct manager (manager.team permission, current <c>ManagerId</c>, not archived).</item>
/// <item><b>Reports, analytics and exports</b>: everyone for an attendance admin; otherwise the caller plus, for a
/// manager, their current direct reports, inside the caller's own organisation.</item>
/// </list>
/// "Attendance admin" is <see cref="IAttendanceAdminAccessService"/>. Every denial throws
/// <c>BusinessRuleException</c> with <see cref="AttendanceAccessMessages.Denied"/>.
/// </summary>
public interface IAttendanceEmployeeAccessService
{
    /// <summary>
    /// Works out the employee a self action is for. <paramref name="requestedEmployeeId"/> null or 0 means "not sent".
    /// Not sent or the caller's own id: the caller's linked employee. Another id: allowed for attendance admins only.
    /// An unlinked attendance admin who sends no id gets <see cref="AttendanceAccessMessages.ChooseEmployee"/>.
    /// </summary>
    Task<AttendanceActionTarget> ResolveActionTargetAsync(int? requestedEmployeeId, CancellationToken cancellationToken = default);

    /// <summary>Allows attendance admins only (manual entries).</summary>
    Task EnsureIsAttendanceAdminAsync(CancellationToken cancellationToken = default);

    /// <summary>Allows self, an attendance admin or the current direct manager to read the employee's records and summary.</summary>
    Task EnsureCanViewEmployeeAttendanceAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The allowed employee set for a report, analytics or export call. The one place report access is decided, so
    /// no report can be missed. A requested employee outside the set, or another organisation for a non-admin, is
    /// refused. A requested employee inside the set narrows the scope to that employee.
    /// </summary>
    Task<AttendanceReportScope> ResolveReportScopeAsync(
        int organizationId,
        int? employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the user may record and read other employees' attendance, which means the user is an attendance
    /// admin. Drives the employee portal's <c>canManageOtherEmployeesAttendance</c> flag.
    /// </summary>
    Task<bool> CanManageOtherEmployeesAttendanceAsync(CancellationToken cancellationToken = default);
}
