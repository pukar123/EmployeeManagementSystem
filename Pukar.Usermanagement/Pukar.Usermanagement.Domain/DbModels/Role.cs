namespace Pukar.Usermanagement.Domain.DbModels;

public class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Upper-invariant name for lookups (e.g. ADMIN).</summary>
    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>When true, the role cannot be deleted via API.</summary>
    public bool IsSystem { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
