namespace Pukar.Usermanagement.Application.DTOs.Auth;

public sealed class ChangePasswordRequestModel
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
