namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// Ready-made callers for access tests.
/// A user that is not linked gets a fresh user id that no seeded employee uses.
/// </summary>
public static class TestUsers
{
    private static int _nextUnlinkedUserId = 900_000;

    /// <summary>No token at all.</summary>
    public static TestUser Anonymous => new("Anonymous (no token)", NextUnlinkedUserId(), [], TestTokenKind.None);

    /// <summary>Signed in, no roles, no linked employee.</summary>
    public static TestUser NoRole => new("Signed-in user with no role and no link", NextUnlinkedUserId(), []);

    /// <summary>Role "Admin", no linked employee.</summary>
    public static TestUser Admin => new("Admin (no link)", NextUnlinkedUserId(), ["Admin"]);

    /// <summary>Role "Admin" in a token signed with a key the API does not trust.</summary>
    public static TestUser AdminWithUntrustedSigningKey =>
        new("Admin with token signed by an untrusted key", NextUnlinkedUserId(), ["Admin"], TestTokenKind.UntrustedSigningKey);

    /// <summary>Role "Employee", linked to <paramref name="linkedTo"/>.</summary>
    public static TestUser Employee(SeededEmployee linkedTo) => LinkedTo(linkedTo, "Employee");

    /// <summary>Role "Manager", linked to <paramref name="linkedTo"/>.</summary>
    public static TestUser Manager(SeededEmployee linkedTo) => LinkedTo(linkedTo, "Manager");

    /// <summary>Any role names, linked to <paramref name="linkedTo"/>.</summary>
    public static TestUser LinkedTo(SeededEmployee linkedTo, params string[] roleNames)
    {
        var roles = roleNames.Length == 0 ? "no role" : $"role {string.Join(", ", roleNames)}";
        return new TestUser($"User with {roles}, linked to {linkedTo.Label}", linkedTo.UserId, roleNames);
    }

    /// <summary>Any role names, no linked employee.</summary>
    public static TestUser Unlinked(params string[] roleNames)
    {
        var roles = roleNames.Length == 0 ? "no role" : $"role {string.Join(", ", roleNames)}";
        return new TestUser($"User with {roles}, no link", NextUnlinkedUserId(), roleNames);
    }

    private static int NextUnlinkedUserId() => Interlocked.Increment(ref _nextUnlinkedUserId);
}
