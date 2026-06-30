using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class EmployeeEmploymentStatusHistory
{
    public long Id { get; set; }

    public int EmployeeId { get; set; }

    public EmploymentStatus? PreviousStatus { get; set; }

    public EmploymentStatus NewStatus { get; set; }

    public DateTime EffectiveDateUtc { get; set; }

    public string? Reason { get; set; }

    public int? ChangedByUserId { get; set; }

    public string? ChangedByUserName { get; set; }

    public string? ChangedByEmail { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Employee Employee { get; set; } = null!;
}
