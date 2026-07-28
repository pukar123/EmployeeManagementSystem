namespace Pukar.Usermanagement.Contracts.UserRoles;

public sealed class AssignUserRolesRequestModel
{
    public IReadOnlyList<int> RoleIds { get; set; } = Array.Empty<int>();
}
