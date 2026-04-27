using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class LeaveRequest
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public LeaveUnit Unit { get; set; } = LeaveUnit.Days;
    public decimal RequestedAmount { get; set; }
    public string? Reason { get; set; }
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public int? ReviewedByEmployeeId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Organization Organization { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
    public Employee? ReviewedByEmployee { get; set; }
    public ICollection<LeaveRequestAttachment> Attachments { get; set; } = new List<LeaveRequestAttachment>();
}
