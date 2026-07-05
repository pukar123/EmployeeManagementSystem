namespace Pukar.Usermanagement.Contracts.Users;

public sealed class AdminSetPasswordRequestModel
{
    public string NewPassword { get; set; } = string.Empty;

    public bool RequirePasswordChange { get; set; } = true;
}
