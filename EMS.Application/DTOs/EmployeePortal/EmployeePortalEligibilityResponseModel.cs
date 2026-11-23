namespace EMS.Application.DTOs.EmployeePortal;

public class EmployeePortalEligibilityResponseModel
{
    public bool HasLinkedEmployeeProfile { get; set; }

    /// <summary>Set when <see cref="HasLinkedEmployeeProfile"/> is true.</summary>
    public int? LinkedEmployeeId { get; set; }

    public int? LinkedOrganizationId { get; set; }

    /// <summary>
    /// When true, the user may use the main Leave screen to submit or view leave for any employee (Leave menu permission).
    /// </summary>
    public bool CanManageOtherEmployeesLeave { get; set; }

    /// <summary>
    /// When true, the user is an attendance admin (admin, or a role granted the <c>attendance.admin</c> menu) and may
    /// record and read attendance for any employee on the Attendance screens. When false, those screens show only
    /// the user's own attendance (and a manager's direct reports in reports).
    /// </summary>
    public bool CanManageOtherEmployeesAttendance { get; set; }
}
