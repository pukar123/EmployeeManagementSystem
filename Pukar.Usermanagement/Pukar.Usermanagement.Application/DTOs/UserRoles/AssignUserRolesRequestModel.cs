namespace Pukar.Usermanagement.Application.DTOs.UserRoles;

public sealed class AssignUserRolesRequestModel
{
    public IReadOnlyList<int> RoleIds { get; set; } = Array.Empty<int>();
}
