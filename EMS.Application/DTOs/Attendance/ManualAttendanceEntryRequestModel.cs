namespace EMS.Application.DTOs.Attendance;

public class ManualAttendanceEntryRequestModel
{
    public int OrganizationId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime CheckInAtUtc { get; set; }
    public DateTime CheckOutAtUtc { get; set; }
    public string? ManualReason { get; set; }
}
