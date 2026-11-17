using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Manager;

public sealed class ManagerLeaveApprovalItemResponseModel
{
    public int LeaveRequestId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public LeaveUnit Unit { get; set; }
    public decimal RequestedAmount { get; set; }
    public string? Reason { get; set; }
    public LeaveRequestStatus Status { get; set; }
    public DateTime SubmittedAtUtc { get; set; }

    /// <summary>Employee's current available balance for this leave type; null when no balance row exists.</summary>
    public decimal? AvailableBalance { get; set; }
}
