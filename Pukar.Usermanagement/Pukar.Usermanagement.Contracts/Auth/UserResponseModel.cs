namespace Pukar.Usermanagement.Contracts.Auth;

public class UserResponseModel
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }
}
