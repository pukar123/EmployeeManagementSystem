namespace EMS.Domain.DbModels;

public class LeaveBalance
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
    public DateTime BalanceAsOfUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Organization Organization { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}
