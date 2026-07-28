namespace EMS.Application.DTOs.Employee;

public sealed class AssignEmployeeUserRolesRequestModel
{
    public IReadOnlyList<string> RoleKeys { get; set; } = Array.Empty<string>();
}
