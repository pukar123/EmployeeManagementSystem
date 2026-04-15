namespace EMS.API.Options;

/// <summary>
/// Feature flags for staged migration from role-id permissions to role-key permissions.
/// </summary>
public sealed class AuthorizationModeOptions
{
    public const string SectionName = "Authorization";

    public bool UseRoleKeyMapping { get; set; }

    public bool EnableLegacyRoleIdFallback { get; set; } = true;
}
