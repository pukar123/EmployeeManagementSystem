namespace Pukar.Usermanagement.Domain.DbModels;

public class ServiceClient
{
    public int Id { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string SecretHash { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string AllowedScopes { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
}
