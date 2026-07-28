namespace EMS.API.Options;

/// <summary>
/// Admin seed options for the consolidated host. Seeding is opt-in and password reset is
/// off by default so a Development/Docker restart never silently rewrites an admin password.
/// </summary>
public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public bool Enabled { get; set; }

    public bool ResetExistingPassword { get; set; }

    public string Email { get; set; } = "admin@localhost";

    public string Password { get; set; } = "";
}
