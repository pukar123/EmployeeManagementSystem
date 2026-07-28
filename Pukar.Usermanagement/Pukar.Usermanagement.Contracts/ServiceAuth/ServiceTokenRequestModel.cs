namespace Pukar.Usermanagement.Contracts.ServiceAuth;

public sealed class ServiceTokenRequestModel
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}
