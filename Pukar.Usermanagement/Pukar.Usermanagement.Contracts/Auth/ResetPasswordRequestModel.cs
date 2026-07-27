namespace Pukar.Usermanagement.Contracts.Auth;

public sealed class ResetPasswordRequestModel
{
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
