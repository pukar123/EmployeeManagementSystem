using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Attendance;

public class AttendanceRecordResponseModel
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
    public AttendanceSource Source { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? ManualReason { get; set; }
    public int BreakMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public IReadOnlyList<AttendanceBreakResponseModel> Breaks { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
