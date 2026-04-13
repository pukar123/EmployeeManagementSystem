namespace EMS.Domain.DbModels;

public class AttendancePolicy
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int StandardDailyMinutes { get; set; } = 480;
    public int StandardBreakMinutes { get; set; } = 60;
    public bool OvertimeEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Organization Organization { get; set; } = null!;
}
