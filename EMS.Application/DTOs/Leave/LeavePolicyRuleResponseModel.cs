using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Leave;

public class LeavePolicyRuleResponseModel
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int LeaveTypeId { get; set; }
    public decimal AccrualRatePerPeriod { get; set; }
    public AccrualFrequency AccrualFrequency { get; set; }
    public decimal? MaximumCarryForward { get; set; }
    public bool EnableProration { get; set; }
    public bool IsActive { get; set; }
    public DateTime EffectiveFromDateUtc { get; set; }
    public DateTime? EffectiveToDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
