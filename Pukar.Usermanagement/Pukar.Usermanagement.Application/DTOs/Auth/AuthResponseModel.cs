namespace Pukar.Usermanagement.Application.DTOs.Auth;

public class AuthResponseModel
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string TokenType { get; set; } = "Bearer";

    public UserResponseModel User { get; set; } = null!;
}
