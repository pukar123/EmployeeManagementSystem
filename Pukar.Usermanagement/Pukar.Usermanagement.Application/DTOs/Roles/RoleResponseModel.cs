namespace Pukar.Usermanagement.Application.DTOs.Roles;

public sealed class RoleResponseModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Upper-invariant key (e.g. ADMIN), aligned with EMS <c>RoleKeyPermissions.RoleKey</c> and JWT roles claim.</summary>
    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystem { get; set; }
}
