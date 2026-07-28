namespace EMS.Application.DTOs.Employee;

public sealed class TerminateEmployeeRequestModel
{
    public DateTime EffectiveDateUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}
