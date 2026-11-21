using System.Net;
using EMS.API.IntegrationTests.Harness;
using Pukar.Usermanagement.Contracts.Auth;
using Pukar.Usermanagement.Contracts.Invitations;

namespace EMS.API.IntegrationTests.Auth;

/// <summary>
/// B05: forgot-password, refresh, reset-password and invitation acceptance are limited per client IP.
/// This host allows 2 of each per IP (forgot-password per email is high). Each test uses its own IP.
/// </summary>
[TestFixture]
public sealed class OtherAuthEndpointLimitTests : AuthEndpointTestFixture
{
    private const string Password = "B05-Correct-Horse-42!";

    protected override IReadOnlyDictionary<string, string?> HostSettings => new Dictionary<string, string?>
    {
        ["AuthRateLimiting:ForgotPasswordPerIp:PermitLimit"] = "2",
        ["AuthRateLimiting:ForgotPasswordPerIp:WindowSeconds"] = "900",
        ["AuthRateLimiting:ForgotPasswordPerEmail:PermitLimit"] = "100",
        ["AuthRateLimiting:RefreshPerIp:PermitLimit"] = "2",
        ["AuthRateLimiting:RefreshPerIp:WindowSeconds"] = "300",
        ["AuthRateLimiting:ResetPasswordPerIp:PermitLimit"] = "2",
        ["AuthRateLimiting:ResetPasswordPerIp:WindowSeconds"] = "900",
    };

    [Test]
    public async Task B05_11_ForgotPasswordPerIp_ThirdEmailFromOneIpIs429()
    {
        const string ip = "10.11.0.1";

        AssertStatus(await PostAsync("forgot-password for x", ForgotPasswordPath,
            new ForgotPasswordRequestModel { Email = "b05-11-x@ems-tests.invalid" }, ip), HttpStatusCode.NoContent);
        AssertStatus(await PostAsync("forgot-password for y", ForgotPasswordPath,
            new ForgotPasswordRequestModel { Email = "b05-11-y@ems-tests.invalid" }, ip), HttpStatusCode.NoContent);
        var third = await PostAsync("forgot-password for z", ForgotPasswordPath,
            new ForgotPasswordRequestModel { Email = "b05-11-z@ems-tests.invalid" }, ip);

        AssertRateLimited(third);
    }

    [Test]
    public async Task B05_12_RefreshPerIp_ThirdCallIs429()
    {
        const string ip = "10.12.0.1";
        var body = new RefreshTokenRequestModel { RefreshToken = "not-a-real-refresh-token" };

        AssertStatus(await PostAsync("refresh #1", RefreshPath, body, ip), HttpStatusCode.BadRequest);
        AssertStatus(await PostAsync("refresh #2", RefreshPath, body, ip), HttpStatusCode.BadRequest);
        var third = await PostAsync("refresh #3", RefreshPath, body, ip);

        AssertRateLimited(third);
    }

    [Test]
    public async Task B05_13_ResetPasswordPerIp_InvalidTokens_400_400_429()
    {
        const string ip = "10.13.0.1";
        var body = new ResetPasswordRequestModel { Token = "not-a-real-reset-token", NewPassword = Password };

        AssertStatus(await PostAsync("reset-password #1", ResetPasswordPath, body, ip), HttpStatusCode.BadRequest);
        AssertStatus(await PostAsync("reset-password #2", ResetPasswordPath, body, ip), HttpStatusCode.BadRequest);
        var third = await PostAsync("reset-password #3", ResetPasswordPath, body, ip);

        AssertRateLimited(third);
    }

    [Test]
    public async Task B05_13b_AcceptInvitation_HasItsOwnPerIpCounter()
    {
        const string ip = "10.13.0.2";
        var reset = new ResetPasswordRequestModel { Token = "not-a-real-reset-token", NewPassword = Password };
        var accept = new AcceptInvitationRequestModel { Token = "not-a-real-invitation-token", NewPassword = Password };

        // Using up the reset-password permits must not use up invitation acceptance.
        AssertStatus(await PostAsync("reset-password #1", ResetPasswordPath, reset, ip), HttpStatusCode.BadRequest);
        AssertStatus(await PostAsync("reset-password #2", ResetPasswordPath, reset, ip), HttpStatusCode.BadRequest);
        AssertStatus(await PostAsync("accept invitation #1", AcceptInvitationPath, accept, ip), HttpStatusCode.BadRequest);
        AssertStatus(await PostAsync("accept invitation #2", AcceptInvitationPath, accept, ip), HttpStatusCode.BadRequest);
        var third = await PostAsync("accept invitation #3", AcceptInvitationPath, accept, ip);

        AssertRateLimited(third);
    }
}
