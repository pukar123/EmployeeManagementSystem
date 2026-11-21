using System.Net;
using EMS.API.IntegrationTests.Harness;
using Pukar.Usermanagement.Contracts.Auth;
using Pukar.Usermanagement.Contracts.Invitations;

namespace EMS.API.IntegrationTests.Auth;

/// <summary>
/// B05 review C1: the anonymous auth actions refuse a body over 16 KB with 413 before reading it,
/// while normal-sized requests are unaffected. Default settings; each case uses its own IP.
/// </summary>
[TestFixture]
public sealed class AuthRequestSizeTests : AuthEndpointTestFixture
{
    private const int OverLimitCharacters = 17 * 1024;

    private static readonly string Huge = new('x', OverLimitCharacters);

    private static IEnumerable<TestCaseData> OversizedCases()
    {
        yield return OversizedCase("B05-18a", SignInPath, new LoginRequestModel { Email = "b05-18@ems-tests.invalid", Password = Huge });
        yield return OversizedCase("B05-18b", RegisterPath, new RegisterRequestModel { Email = "b05-18@ems-tests.invalid", Password = Huge });
        yield return OversizedCase("B05-18c", RefreshPath, new RefreshTokenRequestModel { RefreshToken = Huge });
        yield return OversizedCase("B05-18d", ForgotPasswordPath, new ForgotPasswordRequestModel { Email = Huge });
        yield return OversizedCase("B05-18e", ResetPasswordPath, new ResetPasswordRequestModel { Token = Huge, NewPassword = "B05-Correct-Horse-42!" });
        yield return OversizedCase("B05-18f", AcceptInvitationPath, new AcceptInvitationRequestModel { Token = Huge, NewPassword = "B05-Correct-Horse-42!" });
    }

    private static int _nextIp;

    [TestCaseSource(nameof(OversizedCases))]
    public async Task OversizedBody_Returns413(string path, object body)
    {
        var ip = $"10.18.0.{Interlocked.Increment(ref _nextIp)}";

        var result = await PostAsync($"oversized body to {path}", path, body, ip);

        AssertStatus(result, HttpStatusCode.RequestEntityTooLarge);
        Assert.That(Parse(result).GetProperty("code").GetString(), Is.EqualTo("request_too_large"));
    }

    [Test]
    public async Task B05_18g_NormalSizedSignIn_IsNotAffected()
    {
        var result = await PostAsync("normal-sized sign-in", SignInPath,
            new LoginRequestModel { Email = "b05-18g@ems-tests.invalid", Password = new string('x', 2000) }, "10.18.1.1");

        AssertStatus(result, HttpStatusCode.BadRequest);
    }

    private static TestCaseData OversizedCase(string id, string path, object body)
        => new TestCaseData(path, body).SetName($"{id} oversized body to {path.Replace("/", "_")} returns 413");
}
