namespace Pukar.Usermanagement.Contracts.Roles;

public sealed class UpdateRoleRequestModel
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
