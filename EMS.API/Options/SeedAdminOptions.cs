namespace EMS.API.Options;

/// <summary>
/// Optional default admin user created on startup when <see cref="Enabled"/> is true and <see cref="Password"/> is non-empty.
/// </summary>
public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public bool Enabled { get; set; }

    public string Email { get; set; } = "admin@localhost";

    public string Password { get; set; } = "";
}
