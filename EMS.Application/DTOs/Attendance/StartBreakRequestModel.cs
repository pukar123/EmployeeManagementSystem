namespace EMS.Application.DTOs.Attendance;

public class StartBreakRequestModel
{
    public int EmployeeId { get; set; }
    public DateTime? StartAtUtc { get; set; }
}
