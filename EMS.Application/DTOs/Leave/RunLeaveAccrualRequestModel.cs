namespace EMS.Application.DTOs.Leave;

public class RunLeaveAccrualRequestModel
{
    public int OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; } = DateTime.UtcNow;
}
