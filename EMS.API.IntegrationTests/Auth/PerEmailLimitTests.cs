using System.Net;
using EMS.API.IntegrationTests.Harness;
using Pukar.Usermanagement.Contracts.Auth;

namespace EMS.API.IntegrationTests.Auth;

/// <summary>
/// B05: sign-in and forgot-password are also limited per normalised email, across all IPs.
/// This host allows 2 per email (per-IP limits are high), and turns the reset cooldown off so the
/// number of reset tokens equals the number of reset emails the API sent.
/// </summary>
[TestFixture]
public sealed class PerEmailLimitTests : AuthEndpointTestFixture
{
    private const string Password = "B05-Correct-Horse-42!";

    protected override IReadOnlyDictionary<string, string?> HostSettings => new Dictionary<string, string?>
    {
        ["AuthRateLimiting:SignInPerIp:PermitLimit"] = "100",
        ["AuthRateLimiting:SignInPerEmail:PermitLimit"] = "2",
        ["AuthRateLimiting:SignInPerEmail:WindowSeconds"] = "300",
        ["AuthRateLimiting:ForgotPasswordPerIp:PermitLimit"] = "100",
        ["AuthRateLimiting:ForgotPasswordPerEmail:PermitLimit"] = "2",
        ["AuthRateLimiting:ForgotPasswordPerEmail:WindowSeconds"] = "3600",
        ["PasswordReset:RequestCooldownSeconds"] = "0",
    };

    [Test]
    public async Task B05_07_SignInPerEmail_CountsAcrossIps_AndOnlyForThatEmail()
    {
        var target = new LoginRequestModel { Email = "b05-07-a@ems-tests.invalid", Password = "Wrong-password-1" };

        AssertStatus(await PostAsync("sign-in #1 for a from IP 1", SignInPath, target, "10.7.0.1"), HttpStatusCode.BadRequest);
        AssertStatus(await PostAsync("sign-in #2 for a from IP 1", SignInPath, target, "10.7.0.1"), HttpStatusCode.BadRequest);
        var fromSecondIp = await PostAsync("sign-in #3 for a from IP 2", SignInPath, target, "10.7.0.2");
        var otherEmail = await PostAsync("sign-in for b from IP 2", SignInPath,
            new LoginRequestModel { Email = "b05-07-b@ems-tests.invalid", Password = "Wrong-password-1" }, "10.7.0.2");

        AssertRateLimited(fromSecondIp);
        AssertStatus(otherEmail, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task B05_08_SignInPerEmail_NormalisesCaseAndSpaces()
    {
        const string ip = "10.8.0.1";

        AssertNotRateLimited(await PostAsync("sign-in as upper case", SignInPath,
            new LoginRequestModel { Email = "B05-08@EMS-Tests.invalid", Password = "Wrong-password-1" }, ip));
        AssertNotRateLimited(await PostAsync("sign-in with spaces", SignInPath,
            new LoginRequestModel { Email = "  b05-08@ems-tests.invalid  ", Password = "Wrong-password-1" }, ip));
        var third = await PostAsync("sign-in as lower case", SignInPath,
            new LoginRequestModel { Email = "b05-08@ems-tests.invalid", Password = "Wrong-password-1" }, ip);

        AssertRateLimited(third);
    }

    [Test]
    public async Task B05_09_ForgotPasswordPerEmail_ThirdRequestIs429_AndSendsNoFurtherEmail()
    {
        const string email = "b05-09@ems-tests.invalid";
        await SeedUserAsync(email, Password);
        var userId = (await FindUserAsync(email))!.Id;
        var body = new ForgotPasswordRequestModel { Email = email };

        AssertStatus(await PostAsync("forgot-password #1", ForgotPasswordPath, body, "10.9.0.1"), HttpStatusCode.NoContent);
        AssertStatus(await PostAsync("forgot-password #2", ForgotPasswordPath, body, "10.9.0.2"), HttpStatusCode.NoContent);
        var tokensBefore = await CountResetTokensAsync(userId);
        var third = await PostAsync("forgot-password #3", ForgotPasswordPath, body, "10.9.0.3");

        AssertRateLimited(third);
        Assert.That(tokensBefore, Is.EqualTo(2), "Each allowed request (cooldown off) creates one reset token and email.");
        Assert.That(await CountResetTokensAsync(userId), Is.EqualTo(tokensBefore),
            "A limited request must not create a reset token or send an email.");
    }

    [Test]
    public async Task B05_10_ForgotPasswordPerEmail_UnknownEmail_GetsTheSameSequence()
    {
        var body = new ForgotPasswordRequestModel { Email = "b05-10-nobody@ems-tests.invalid" };

        AssertStatus(await PostAsync("forgot-password #1 (unknown email)", ForgotPasswordPath, body, "10.10.0.1"), HttpStatusCode.NoContent);
        AssertStatus(await PostAsync("forgot-password #2 (unknown email)", ForgotPasswordPath, body, "10.10.0.1"), HttpStatusCode.NoContent);
        var third = await PostAsync("forgot-password #3 (unknown email)", ForgotPasswordPath, body, "10.10.0.1");

        AssertRateLimited(third);
    }

    private Task<int> CountResetTokensAsync(int userId)
        => Factory.QueryUserManagementDatabaseAsync(db =>
            Task.FromResult(db.PasswordResetTokens.Count(t => t.UserId == userId)));
}
