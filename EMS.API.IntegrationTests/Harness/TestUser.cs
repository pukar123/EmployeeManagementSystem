namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// A caller in an access test: who they are (user id and role names) and which token they send.
/// Use the ready-made users in <see cref="TestUsers"/> where possible.
/// </summary>
/// <param name="Description">Plain description used in failure messages, e.g. "Employee linked to E1".</param>
/// <param name="UserId">Token <c>sub</c>. Equals a seeded employee's user id when the user is linked.</param>
/// <param name="RoleNames">Role names put in the token, e.g. "Admin". EMS upper-cases them into role keys.</param>
/// <param name="Token">Which token the user sends.</param>
public sealed record TestUser(
    string Description,
    int UserId,
    IReadOnlyList<string> RoleNames,
    TestTokenKind Token = TestTokenKind.Trusted)
{
    public string Email => $"user{UserId}@ems-tests.invalid";

    public override string ToString() => Description;
}
