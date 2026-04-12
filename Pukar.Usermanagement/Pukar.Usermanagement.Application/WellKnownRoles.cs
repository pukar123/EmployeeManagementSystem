namespace Pukar.Usermanagement.Application;

/// <summary>Built-in role names used for seeding and policy checks.</summary>
public static class WellKnownRoles
{
    public const string Admin = "Admin";

    /// <summary>Upper-invariant name stored on the Role entity.</summary>
    public const string AdminNormalizedName = "ADMIN";
}
