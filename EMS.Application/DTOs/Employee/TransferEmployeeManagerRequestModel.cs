namespace EMS.Application.DTOs.Employee;

public sealed class TransferEmployeeManagerRequestModel
{
    public int? NewManagerId { get; set; }

    public DateTime EffectiveFromUtc { get; set; }

    public string? Reason { get; set; }
}
