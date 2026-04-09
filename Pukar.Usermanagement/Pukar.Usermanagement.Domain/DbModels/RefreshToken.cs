namespace Pukar.Usermanagement.Domain.DbModels;

public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>SHA-256 hex of the opaque refresh token string.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Optional client hint (user agent, device id).</summary>
    public string? ClientInfo { get; set; }
}
