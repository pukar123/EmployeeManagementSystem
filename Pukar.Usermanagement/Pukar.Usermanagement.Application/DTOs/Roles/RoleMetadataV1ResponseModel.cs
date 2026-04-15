namespace Pukar.Usermanagement.Application.DTOs.Roles;

public sealed class RoleMetadataV1ResponseModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public bool IsSystem { get; set; }
}
