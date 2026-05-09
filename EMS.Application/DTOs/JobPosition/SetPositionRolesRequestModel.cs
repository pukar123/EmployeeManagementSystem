namespace EMS.Application.DTOs.JobPosition;

public sealed class SetPositionRolesRequestModel
{
    public IReadOnlyList<int> RoleIds { get; set; } = Array.Empty<int>();
}
