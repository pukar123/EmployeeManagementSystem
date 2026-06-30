using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class EmployeeScheduledChange
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public EmployeeScheduledChangeType ChangeType { get; set; }

    public int? TargetReferenceId { get; set; }

    public int? TargetStatus { get; set; }

    public DateTime EffectiveAtUtc { get; set; }

    public string? Reason { get; set; }

    public EmployeeScheduledChangeStatus Status { get; set; } = EmployeeScheduledChangeStatus.Pending;

    public int? CreatedByUserId { get; set; }

    public string? CreatedByUserName { get; set; }

    public string? CreatedByEmail { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public string? FailureReason { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Employee Employee { get; set; } = null!;
}
