using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class LeavePolicyRule
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int LeaveTypeId { get; set; }
    public decimal AccrualRatePerPeriod { get; set; }
    public AccrualFrequency AccrualFrequency { get; set; } = AccrualFrequency.Monthly;
    public decimal? MaximumCarryForward { get; set; }
    public bool EnableProration { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFromDateUtc { get; set; }
    public DateTime? EffectiveToDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Organization Organization { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}
