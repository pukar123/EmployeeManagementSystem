namespace EMS.Application.DTOs.Employee;

public sealed class SetEmployeeDirectRolesRequestModel
{
    public IReadOnlyList<int> RoleIds { get; set; } = Array.Empty<int>();
}
