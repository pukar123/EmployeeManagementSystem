namespace EMS.Application.DTOs.Employee;

public sealed class ProvisionEmployeeUserResponseModel
{
    public string EmployeeName { get; set; } = string.Empty;

    public string EmployeeNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsNewUser { get; set; }

    public IReadOnlyList<string> AssignedRoleKeys { get; set; } = Array.Empty<string>();
}
