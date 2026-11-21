using System.Net;
using EMS.API.IntegrationTests.Harness;
using Pukar.Usermanagement.Contracts.Auth;
using Pukar.Usermanagement.Contracts.Invitations;

namespace EMS.API.IntegrationTests.Auth;

/// <summary>
/// B05: with default settings public self-registration is closed (404, nothing created), while
/// invitation acceptance and sign-in keep working. Default rate limits apply and are not reached.
/// </summary>
[TestFixture]
public sealed class RegistrationClosedTests : AuthEndpointTestFixture
{
    private const string Password = "B05-Correct-Horse-42!";

    [Test]
    public async Task B05_01_Register_WithValidNewAccount_Returns404AndCreatesNoUser()
    {
        const string email = "b05-01-new@ems-tests.invalid";

        var result = await PostAsync("register a new account", RegisterPath,
            new RegisterRequestModel { Email = email, Password = Password, UserName = "New Person" }, "10.1.0.1");

        AssertRegistrationDisabled(result);
        Assert.That(await FindUserAsync(email), Is.Null, "No user may be created when registration is closed.");
    }

    [Test]
    public async Task B05_02_Register_WithAnExistingEmail_Returns404NotConflict()
    {
        const string email = "b05-02-existing@ems-tests.invalid";
        await SeedUserAsync(email, Password);

        var existing = await PostAsync("register an existing email", RegisterPath,
            new RegisterRequestModel { Email = email, Password = Password }, "10.1.0.2");
        var unknown = await PostAsync("register a new email", RegisterPath,
            new RegisterRequestModel { Email = "b05-02-new@ems-tests.invalid", Password = Password }, "10.1.0.2");

        AssertRegistrationDisabled(existing);
        Assert.That(existing.Body, Is.EqualTo(unknown.Body),
            "The answer must not reveal whether the email already has an account.");
    }

    [Test]
    public async Task B05_03_Register_WithEmptyBody_Returns404()
    {
        var result = await PostEmptyJsonAsync("register with an empty body", RegisterPath, "10.1.0.3");

        AssertRegistrationDisabled(result);
    }

    [Test]
    public async Task B05_04_AcceptInvitation_ActivatesUser_WhoCanThenSignIn()
    {
        const string email = "b05-04-invitee@ems-tests.invalid";
        var rawToken = await SeedInvitationAsync(email);

        var accept = await PostAsync("accept invitation", AcceptInvitationPath,
            new AcceptInvitationRequestModel { Token = rawToken, NewPassword = Password }, "10.1.0.4");

        AssertStatus(accept, HttpStatusCode.NoContent);
        var user = await FindUserAsync(email);
        Assert.That(user, Is.Not.Null);
        Assert.That(user!.IsActive, Is.True, "Accepting the invitation must activate the user.");

        var signIn = await PostAsync("sign in after accepting", SignInPath,
            new LoginRequestModel { Email = email, Password = Password }, "10.1.0.4");

        AssertStatus(signIn, HttpStatusCode.OK);
        var json = Parse(signIn);
        Assert.That(json.GetProperty("accessToken").GetString(), Is.Not.Empty);
        Assert.That(json.GetProperty("refreshToken").GetString(), Is.Not.Empty);
    }

    private static void AssertRegistrationDisabled(AuthCallResult result)
    {
        AssertStatus(result, HttpStatusCode.NotFound);
        var json = Parse(result);
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("code").GetString(), Is.EqualTo("registration_disabled"), $"{result.Description}: code");
            Assert.That(json.GetProperty("message").GetString(),
                Is.EqualTo("Self-registration is not available. Ask your administrator for an invitation."),
                $"{result.Description}: message");
            Assert.That(json.TryGetProperty("accessToken", out _), Is.False, $"{result.Description}: no tokens may be issued");
        });
    }
}
