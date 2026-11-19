namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// What kind of bearer token a <see cref="TestUser"/> sends.
/// </summary>
public enum TestTokenKind
{
    /// <summary>No Authorization header at all.</summary>
    None,

    /// <summary>A real access token from the API's own <c>IJwtTokenService</c>.</summary>
    Trusted,

    /// <summary>An otherwise valid token signed with a key the API does not trust.</summary>
    UntrustedSigningKey,
}
