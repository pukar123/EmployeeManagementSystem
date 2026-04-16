namespace EMS.Application.DTOs.Employee;

public sealed class ProvisionEmployeeUserResponseModel
{
    public string EmployeeName { get; set; } = string.Empty;

    public string EmployeeNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? TemporaryPassword { get; set; }

    public bool IsNewUser { get; set; }

    public IReadOnlyList<int> AssignedRoleIds { get; set; } = Array.Empty<int>();
}
