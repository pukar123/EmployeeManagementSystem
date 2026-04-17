namespace EMS.Domain.DbModels;

public class EmployeeDepartmentHistory
{
    public long Id { get; set; }

    public int EmployeeId { get; set; }

    public int? PreviousDepartmentId { get; set; }

    public int? NewDepartmentId { get; set; }

    public DateTime EffectiveFromUtc { get; set; }

    public DateTime? EffectiveToUtc { get; set; }

    public string? Reason { get; set; }

    public int? ChangedByUserId { get; set; }

    public string? ChangedByUserName { get; set; }

    public string? ChangedByEmail { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Employee Employee { get; set; } = null!;
}
