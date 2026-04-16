namespace EMS.Application.DTOs.Employee;

public sealed class AssignEmployeeUserRolesRequestModel
{
    public IReadOnlyList<int> RoleIds { get; set; } = Array.Empty<int>();
}
