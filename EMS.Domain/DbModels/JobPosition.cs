namespace EMS.Domain.DbModels;

public class JobPosition
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;

    public Organization Organization { get; set; } = null!;
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<PositionRole> PositionRoles { get; set; } = new List<PositionRole>();
    public ICollection<EmployeeRoleAssignment> EmployeeRoleAssignments { get; set; } = new List<EmployeeRoleAssignment>();
}
