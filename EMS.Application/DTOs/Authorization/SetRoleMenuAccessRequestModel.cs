namespace EMS.Application.DTOs.Authorization;

public sealed class SetRoleMenuAccessRequestModel
{
    public IReadOnlyList<int> MenuIds { get; set; } = Array.Empty<int>();
}
