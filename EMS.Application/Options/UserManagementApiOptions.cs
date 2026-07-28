namespace EMS.Application.Options;

public sealed class UserManagementApiOptions
{
    public const string SectionName = "UserManagementApi";

    /// <summary>
    /// Base URL for Pukar.Usermanagement.Host (e.g. https://localhost:7098).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Relative path for internal role metadata.
    /// </summary>
    public string RolesMetadataPath { get; set; } = "/api/internal/v1/roles/metadata";

    /// <summary>
    /// Outbound request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Service client id registered in User Management (ServiceClients:Ems).
    /// </summary>
    public string ServiceClientId { get; set; } = "ems";

    /// <summary>
    /// Service client secret. Prefer user-secrets / environment variables in non-dev.
    /// </summary>
    public string ServiceClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Retry count for safe/idempotent GET operations only.
    /// </summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff on safe retries.
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 200;

    /// <summary>
    /// JWT issuer expected on user tokens (must match User Management Jwt:Issuer).
    /// </summary>
    public string JwtIssuer { get; set; } = "Pukar.Usermanagement";

    /// <summary>
    /// JWT audience expected on user tokens (must match User Management Jwt:Audience).
    /// </summary>
    public string JwtAudience { get; set; } = "ems";

    /// <summary>
    /// Relative JWKS path on User Management Host.
    /// </summary>
    public string JwksPath { get; set; } = "/.well-known/jwks.json";
}
