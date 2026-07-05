namespace Pukar.Usermanagement.Contracts.ServiceAuth;

public sealed class ServiceTokenResponseModel
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public int ExpiresInSeconds { get; set; }

    public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
}
