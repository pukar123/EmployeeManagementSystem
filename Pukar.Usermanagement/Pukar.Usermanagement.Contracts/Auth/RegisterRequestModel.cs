namespace Pukar.Usermanagement.Contracts.Auth;

public class RegisterRequestModel
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? UserName { get; set; }
}
