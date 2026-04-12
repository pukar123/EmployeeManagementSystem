namespace EMS.Application.DTOs.Navigation;

public sealed class SetRolePermissionsRequestModel
{
    public IReadOnlyList<RolePermissionItemModel> Permissions { get; set; } = Array.Empty<RolePermissionItemModel>();
}
