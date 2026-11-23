namespace EMS.Application.DTOs.Attendance;

public class EndBreakRequestModel
{
    /// <summary>
    /// Optional. Leave out (or send the caller's own id) to act for the caller's linked employee. Another
    /// employee's id is accepted from attendance admins only; anyone else gets 403.
    /// </summary>
    public int? EmployeeId { get; set; }
    /// <summary>Used for attendance admins only. For everyone else the server's current UTC time is used.</summary>
    public DateTime? EndAtUtc { get; set; }
}
