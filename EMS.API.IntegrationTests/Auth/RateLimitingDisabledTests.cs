using EMS.API.IntegrationTests.Harness;
using Pukar.Usermanagement.Contracts.Auth;

namespace EMS.API.IntegrationTests.Auth;

/// <summary>B05: <c>AuthRateLimiting:Enabled = false</c> turns every auth limit off, even very low ones.</summary>
[TestFixture]
public sealed class RateLimitingDisabledTests : AuthEndpointTestFixture
{
    protected override IReadOnlyDictionary<string, string?> HostSettings => new Dictionary<string, string?>
    {
        ["AuthRateLimiting:Enabled"] = "false",
        ["AuthRateLimiting:SignInPerIp:PermitLimit"] = "1",
        ["AuthRateLimiting:SignInPerEmail:PermitLimit"] = "1",
    };

    [Test]
    public async Task B05_14_WhenDisabled_RepeatedSignInsAreNeverLimited()
    {
        var body = new LoginRequestModel { Email = "b05-14@ems-tests.invalid", Password = "Wrong-password-1" };

        for (var attempt = 1; attempt <= 3; attempt++)
            AssertNotRateLimited(await PostAsync($"sign-in #{attempt}", SignInPath, body, "10.14.0.1"));
    }
}
