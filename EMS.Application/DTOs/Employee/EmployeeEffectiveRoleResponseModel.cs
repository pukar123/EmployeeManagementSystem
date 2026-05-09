namespace EMS.Application.DTOs.Employee;

public sealed class EmployeeEffectiveRoleResponseModel
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleNormalizedName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int? JobPositionId { get; set; }
    public string? JobPositionTitle { get; set; }
    public bool IsSystem { get; set; }
}
