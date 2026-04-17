namespace EMS.Application.DTOs.Authorization;

public sealed class RoleMenuAccessResponseModel
{
    public string RoleKey { get; set; } = string.Empty;

    public IReadOnlyList<int> MenuIds { get; set; } = Array.Empty<int>();
}
