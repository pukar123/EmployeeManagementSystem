namespace Pukar.Usermanagement.Domain.DbModels;

public class PasswordResetToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>SHA-256 hex of the opaque reset token sent by email.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public User User { get; set; } = null!;
}
