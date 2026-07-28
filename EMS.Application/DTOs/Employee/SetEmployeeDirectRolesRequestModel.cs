namespace EMS.Application.DTOs.Employee;

public sealed class SetEmployeeDirectRolesRequestModel
{
    public IReadOnlyList<string> RoleKeys { get; set; } = Array.Empty<string>();
}
