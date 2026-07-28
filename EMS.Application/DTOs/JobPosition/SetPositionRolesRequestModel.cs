namespace EMS.Application.DTOs.JobPosition;

public sealed class SetPositionRolesRequestModel
{
    public IReadOnlyList<string> RoleKeys { get; set; } = Array.Empty<string>();
}
