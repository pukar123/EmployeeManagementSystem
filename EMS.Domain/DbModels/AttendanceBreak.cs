namespace EMS.Domain.DbModels;

public class AttendanceBreak
{
    public int Id { get; set; }
    public int AttendanceRecordId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public int? DurationMinutes { get; set; }

    public AttendanceRecord AttendanceRecord { get; set; } = null!;
}
