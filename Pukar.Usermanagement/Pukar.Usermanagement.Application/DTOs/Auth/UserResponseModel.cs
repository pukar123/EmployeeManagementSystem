namespace Pukar.Usermanagement.Application.DTOs.Auth;

public class UserResponseModel
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }
}
