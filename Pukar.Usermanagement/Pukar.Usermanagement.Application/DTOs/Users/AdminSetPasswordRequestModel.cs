namespace Pukar.Usermanagement.Application.DTOs.Users;

public sealed class AdminSetPasswordRequestModel
{
    public string NewPassword { get; set; } = string.Empty;
}
