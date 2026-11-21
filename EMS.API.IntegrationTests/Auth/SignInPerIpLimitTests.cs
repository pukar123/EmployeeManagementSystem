using System.Net;
using EMS.API.IntegrationTests.Harness;
using Pukar.Usermanagement.Contracts.Auth;

namespace EMS.API.IntegrationTests.Auth;

/// <summary>
/// B05: sign-in is limited per client IP. This host allows 3 sign-ins per IP per 5 minutes and a high
/// per-email limit, so only the per-IP limit is in play. Each test uses its own IP.
/// </summary>
[TestFixture]
public sealed class SignInPerIpLimitTests : AuthEndpointTestFixture
{
    private const string Password = "B05-Correct-Horse-42!";

    protected override IReadOnlyDictionary<string, string?> HostSettings => new Dictionary<string, string?>
    {
        ["AuthRateLimiting:SignInPerIp:PermitLimit"] = "3",
        ["AuthRateLimiting:SignInPerIp:WindowSeconds"] = "300",
        ["AuthRateLimiting:SignInPerEmail:PermitLimit"] = "100",
        ["AuthRateLimiting:SignInPerEmail:WindowSeconds"] = "300",
    };

    [Test]
    public async Task B05_05_FourthSignInFromOneIp_Returns429_EvenWithTheRightPassword()
    {
        const string email = "b05-05@ems-tests.invalid";
        await SeedUserAsync(email, Password);

        var rejected = await FailThreeTimesThenSignInAsync(email, "10.5.0.5");

        AssertRateLimited(rejected);
    }

    [Test]
    public async Task B05_06_UnknownEmail_GetsTheSameSequenceAndTheSame429()
    {
        const string known = "b05-06-known@ems-tests.invalid";
        await SeedUserAsync(known, Password);

        var knownRejected = await FailThreeTimesThenSignInAsync(known, "10.5.0.61");
        var unknownRejected = await FailThreeTimesThenSignInAsync("b05-06-nobody@ems-tests.invalid", "10.5.0.62");

        var knownJson = AssertRateLimited(knownRejected);
        var unknownJson = AssertRateLimited(unknownRejected);
        Assert.That(
            unknownJson.EnumerateObject().Select(p => p.Name),
            Is.EqualTo(knownJson.EnumerateObject().Select(p => p.Name)),
            "The 429 must look the same whether or not the account exists.");
    }

    [Test]
    public async Task B05_15_CrossOrigin429_CarriesCorsHeaders_AndExposesRetryAfter()
    {
        const string ip = "10.5.0.15";
        var body = new LoginRequestModel { Email = "b05-15@ems-tests.invalid", Password = "Wrong-password-1" };
        for (var attempt = 1; attempt <= 3; attempt++)
            AssertNotRateLimited(await PostAsync($"cross-origin sign-in #{attempt}", SignInPath, body, ip, AllowedOrigin));

        var rejected = await PostAsync("cross-origin sign-in #4", SignInPath, body, ip, AllowedOrigin);

        AssertRateLimited(rejected);
        Assert.That(rejected.Header("Access-Control-Allow-Origin"), Is.EqualTo(AllowedOrigin),
            $"{rejected.Description}: the browser can only read a 429 that carries CORS headers.");
        Assert.That(rejected.Header("Access-Control-Expose-Headers"), Does.Contain("Retry-After"),
            $"{rejected.Description}: Retry-After must be exposed to the browser.");
    }

    [Test]
    public async Task B05_16_AfterSignInIsLimited_OtherEndpointsStillWork()
    {
        const string ip = "10.5.0.16";
        var body = new LoginRequestModel { Email = "b05-16@ems-tests.invalid", Password = "Wrong-password-1" };
        for (var attempt = 1; attempt <= 3; attempt++)
            AssertNotRateLimited(await PostAsync($"sign-in #{attempt}", SignInPath, body, ip));
        AssertRateLimited(await PostAsync("sign-in #4", SignInPath, body, ip));

        var organisations = await GetAsAsync("Admin lists organisations from the same IP", TestUsers.Admin, "/api/Organizations", ip);

        AssertStatus(organisations, HttpStatusCode.OK);
    }

    private async Task<AuthCallResult> FailThreeTimesThenSignInAsync(string email, string ip)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var wrong = await PostAsync($"wrong-password sign-in #{attempt} for {email}", SignInPath,
                new LoginRequestModel { Email = email, Password = "Wrong-password-1" }, ip);
            AssertStatus(wrong, HttpStatusCode.BadRequest);
        }

        return await PostAsync($"sign-in #4 for {email} with the right password", SignInPath,
            new LoginRequestModel { Email = email, Password = Password }, ip);
    }
}
