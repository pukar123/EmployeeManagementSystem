namespace EMS.Application.DTOs.Leave;

public class LeaveBalanceResponseModel
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal AccruedAmount { get; set; }
    public decimal UsedAmount { get; set; }
    public decimal AdjustedAmount { get; set; }
    public decimal CarryForwardAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public DateTime BalanceAsOfUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
