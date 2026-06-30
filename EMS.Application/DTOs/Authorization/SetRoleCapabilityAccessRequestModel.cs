namespace EMS.Application.DTOs.Authorization;

public sealed class SetRoleCapabilityAccessRequestModel
{
    public IReadOnlyList<string>? CapabilityKeys { get; set; }
}
