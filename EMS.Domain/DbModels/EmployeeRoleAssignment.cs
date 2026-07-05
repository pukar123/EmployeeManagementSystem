using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class EmployeeRoleAssignment
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string RoleKey { get; set; } = string.Empty;
    public EmployeeRoleSource Source { get; set; } = EmployeeRoleSource.DirectOverride;
    public int? JobPositionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Employee Employee { get; set; } = null!;
    public JobPosition? JobPosition { get; set; }
}
