namespace EMS.Domain.DbModels;

public class EmployeeManagerHistory
{
    public long Id { get; set; }

    public int EmployeeId { get; set; }

    public int? PreviousManagerId { get; set; }

    public int? NewManagerId { get; set; }

    public DateTime EffectiveFromUtc { get; set; }

    public DateTime? EffectiveToUtc { get; set; }

    public string? Reason { get; set; }

    public int? ChangedByUserId { get; set; }

    public string? ChangedByUserName { get; set; }

    public string? ChangedByEmail { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Employee Employee { get; set; } = null!;
}
