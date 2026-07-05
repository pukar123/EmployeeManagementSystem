namespace Pukar.Usermanagement.Host.Options;

public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public bool Enabled { get; set; }

    public string Email { get; set; } = "admin@localhost";

    public string Password { get; set; } = "";
}
