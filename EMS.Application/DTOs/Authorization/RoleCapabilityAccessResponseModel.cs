namespace EMS.Application.DTOs.Authorization;

public sealed class RoleCapabilityAccessResponseModel
{
    public string RoleKey { get; set; } = string.Empty;

    public IReadOnlyList<string> CapabilityKeys { get; set; } = Array.Empty<string>();
}
