using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class LeaveType
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LeaveUnit Unit { get; set; } = LeaveUnit.Days;
    public bool RequiresAttachment { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<LeavePolicyRule> PolicyRules { get; set; } = new List<LeavePolicyRule>();
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
