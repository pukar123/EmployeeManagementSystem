namespace EMS.Application.DTOs.JobPosition;

public sealed class PositionRoleResponseModel
{
    public string RoleKey { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string RoleNormalizedName { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}
