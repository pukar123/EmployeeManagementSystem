using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Employee;

public sealed class CreateEmployeeScheduledChangeRequestModel
{
    public EmployeeScheduledChangeType ChangeType { get; set; }

    public DateTime EffectiveAtUtc { get; set; }

    public string? Reason { get; set; }

    public int? TargetReferenceId { get; set; }

    public EmploymentStatus? TargetStatus { get; set; }
}

public sealed class EmployeeScheduledChangeResponseModel
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public EmployeeScheduledChangeType ChangeType { get; set; }

    public int? TargetReferenceId { get; set; }

    public EmploymentStatus? TargetStatus { get; set; }

    public DateTime EffectiveAtUtc { get; set; }

    public string? Reason { get; set; }

    public EmployeeScheduledChangeStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public string? FailureReason { get; set; }
}
