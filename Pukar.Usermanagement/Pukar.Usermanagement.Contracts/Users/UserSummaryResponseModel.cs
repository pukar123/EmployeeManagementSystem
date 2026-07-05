namespace Pukar.Usermanagement.Contracts.Users;

public sealed class UserSummaryResponseModel
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }
}
