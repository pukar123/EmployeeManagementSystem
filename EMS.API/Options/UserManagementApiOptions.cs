namespace EMS.API.Options;

public sealed class UserManagementApiOptions
{
    public const string SectionName = "UserManagementApi";

    /// <summary>
    /// Base URL for UserManagement API when running as a separate service.
    /// Keep empty to disable outbound metadata calls.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Relative metadata endpoint path.
    /// </summary>
    public string RolesMetadataPath { get; set; } = "/api/roles/metadata/v1";

    /// <summary>
    /// Outbound request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
