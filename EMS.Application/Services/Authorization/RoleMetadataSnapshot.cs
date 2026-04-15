namespace EMS.Application.Services.Authorization;

public sealed class RoleMetadataSnapshot
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string NormalizedName { get; init; } = string.Empty;

    public bool IsSystem { get; init; }
}
