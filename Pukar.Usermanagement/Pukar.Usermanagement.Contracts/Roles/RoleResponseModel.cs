namespace Pukar.Usermanagement.Contracts.Roles;

public sealed class RoleResponseModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystem { get; set; }
}
