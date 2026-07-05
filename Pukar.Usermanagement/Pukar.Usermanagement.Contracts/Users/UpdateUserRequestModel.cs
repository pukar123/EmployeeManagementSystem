namespace Pukar.Usermanagement.Contracts.Users;

public sealed class UpdateUserRequestModel
{
    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public bool IsActive { get; set; }
}
