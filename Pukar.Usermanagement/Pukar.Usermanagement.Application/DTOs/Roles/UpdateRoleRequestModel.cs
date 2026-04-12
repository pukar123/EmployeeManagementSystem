namespace Pukar.Usermanagement.Application.DTOs.Roles;

public sealed class UpdateRoleRequestModel
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
