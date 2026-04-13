namespace EMS.Application.DTOs.Attendance;

public class EndBreakRequestModel
{
    public int EmployeeId { get; set; }
    public DateTime? EndAtUtc { get; set; }
}
