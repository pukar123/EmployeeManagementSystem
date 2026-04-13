namespace EMS.Application.DTOs.Attendance;

public class AttendanceBreakResponseModel
{
    public int Id { get; set; }
    public int AttendanceRecordId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public int? DurationMinutes { get; set; }
}
