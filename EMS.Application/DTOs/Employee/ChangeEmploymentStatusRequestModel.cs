namespace EMS.Application.DTOs.Employee;

public sealed class ChangeEmploymentStatusRequestModel
{
    public EMS.Domain.Enums.EmploymentStatus NewStatus { get; set; }
    public DateTime EffectiveDateUtc { get; set; }
    public string? Reason { get; set; }
}
