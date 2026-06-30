namespace EMS.Application.DTOs.Employee;

public sealed class TransferEmployeeDepartmentRequestModel
{
    public int? NewDepartmentId { get; set; }

    public DateTime EffectiveFromUtc { get; set; }

    public string? Reason { get; set; }
}
