using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Leave;

public class CreateLeaveRequestRequestModel
{
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public LeaveUnit Unit { get; set; } = LeaveUnit.Days;
    public decimal RequestedAmount { get; set; }
    public string? Reason { get; set; }
}
