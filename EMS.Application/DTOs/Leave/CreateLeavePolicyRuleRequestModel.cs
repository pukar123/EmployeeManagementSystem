using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Leave;

public class CreateLeavePolicyRuleRequestModel
{
    public int OrganizationId { get; set; }
    public int LeaveTypeId { get; set; }
    public decimal AccrualRatePerPeriod { get; set; }
    public AccrualFrequency AccrualFrequency { get; set; } = AccrualFrequency.Monthly;
    public decimal? MaximumCarryForward { get; set; }
    public bool EnableProration { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFromDateUtc { get; set; }
    public DateTime? EffectiveToDateUtc { get; set; }
}
