namespace Pukar.Usermanagement.Application.Options;

public class JwtTokenOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Pukar.Usermanagement";

    public string Audience { get; set; } = "ems";

    /// <summary>RSA private key PEM for RS256 signing.</summary>
    public string SigningKeyPem { get; set; } = string.Empty;

    /// <summary>Optional path to PEM file (used when SigningKeyPem is empty).</summary>
    public string SigningKeyPemFile { get; set; } = string.Empty;

    public string SigningKeyId { get; set; } = "um-key-1";

    /// <summary>Legacy symmetric key for embedded host backward compatibility.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;

    public int ServiceTokenExpirationMinutes { get; set; } = 5;
}
