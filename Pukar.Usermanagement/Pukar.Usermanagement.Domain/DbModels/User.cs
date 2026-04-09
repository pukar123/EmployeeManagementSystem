namespace Pukar.Usermanagement.Domain.DbModels;

public class User
{
    public int Id { get; set; }

    /// <summary>Original email as entered (display).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Upper-invariant email for unique lookup.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
