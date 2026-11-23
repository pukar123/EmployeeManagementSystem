namespace EMS.Application.Services.Attendance;

/// <summary>
/// Which employees' records a report, analytics or export call may include, as decided by
/// <see cref="IAttendanceEmployeeAccessService.ResolveReportScopeAsync"/>. The request's own filters can only narrow it.
/// </summary>
public sealed class AttendanceReportScope
{
    private AttendanceReportScope(IReadOnlyCollection<int>? employeeIds)
    {
        EmployeeIds = employeeIds;
    }

    /// <summary>Attendance admins: every employee in the requested organisation.</summary>
    public static AttendanceReportScope Unrestricted { get; } = new(null);

    /// <summary>
    /// The allowed employee ids, or null when the scope is <see cref="Unrestricted"/>.
    /// Passed into the database query, never applied after loading.
    /// </summary>
    public IReadOnlyCollection<int>? EmployeeIds { get; }

    public bool IsUnrestricted => EmployeeIds is null;

    public static AttendanceReportScope Only(IEnumerable<int> employeeIds)
        => new(employeeIds.Distinct().ToArray());
}
