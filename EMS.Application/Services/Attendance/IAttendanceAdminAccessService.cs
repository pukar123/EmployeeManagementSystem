namespace EMS.Application.Services.Attendance;

/// <summary>
/// The single definition of "attendance admin" (B07): a signed-in user who is an admin, or whose roles are granted
/// the <c>attendance.admin</c> menu. The <c>attendance</c> page menu and its <c>attendance.reports</c> and
/// <c>attendance.analytics</c> children do not count; they only control navigation.
/// Used by <see cref="IAttendanceEmployeeAccessService"/>, the employee portal's attendance flag and the API's
/// <c>AttendanceAdmin</c> authorisation policy, so all of them always give the same answer.
/// </summary>
public interface IAttendanceAdminAccessService
{
    /// <summary>True when the current user is an attendance admin. Admins are answered without a menu query.</summary>
    Task<bool> IsAttendanceAdminAsync(CancellationToken cancellationToken = default);
}
