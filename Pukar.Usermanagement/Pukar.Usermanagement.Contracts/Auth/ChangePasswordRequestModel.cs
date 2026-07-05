namespace Pukar.Usermanagement.Contracts.Auth;

public sealed class ChangePasswordRequestModel
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
