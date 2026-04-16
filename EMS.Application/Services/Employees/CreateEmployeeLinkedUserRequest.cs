namespace EMS.Application.Services.Employees;

public sealed class CreateEmployeeLinkedUserRequest
{
    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; } = true;
}
