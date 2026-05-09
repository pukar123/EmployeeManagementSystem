namespace EMS.Application.DTOs.JobPosition;

public sealed class PositionRoleResponseModel
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleNormalizedName { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}
