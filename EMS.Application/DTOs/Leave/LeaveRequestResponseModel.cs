using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Leave;

public class LeaveRequestResponseModel
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public LeaveUnit Unit { get; set; }
    public decimal RequestedAmount { get; set; }
    public string? Reason { get; set; }
    public LeaveRequestStatus Status { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public int? ReviewedByEmployeeId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
