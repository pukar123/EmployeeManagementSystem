namespace Pukar.Usermanagement.Application.DTOs.Users;

public sealed class CreateUserRequestModel
{
    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
