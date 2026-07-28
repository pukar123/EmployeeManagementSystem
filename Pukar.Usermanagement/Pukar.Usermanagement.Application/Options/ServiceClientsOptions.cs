namespace Pukar.Usermanagement.Application.Options;

public sealed class ServiceClientsOptions
{
    public const string SectionName = "ServiceClients";

    public ServiceClientSeedOptions Ems { get; set; } = new();
}

public sealed class ServiceClientSeedOptions
{
    public string ClientId { get; set; } = "ems";

    public string Secret { get; set; } = string.Empty;

    public string Name { get; set; } = "EMS.API";
}
