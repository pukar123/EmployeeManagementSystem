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
}
