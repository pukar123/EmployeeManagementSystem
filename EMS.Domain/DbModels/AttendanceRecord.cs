using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class AttendanceRecord
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime CheckInAtUtc { get; set; }
    public DateTime? CheckOutAtUtc { get; set; }
    public decimal? CheckInLatitude { get; set; }
    public decimal? CheckInLongitude { get; set; }
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }
    public AttendanceSource Source { get; set; } = AttendanceSource.Web;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Open;
    public string? ManualReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Organization Organization { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public ICollection<AttendanceBreak> Breaks { get; set; } = new List<AttendanceBreak>();
}
