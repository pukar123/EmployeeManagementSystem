namespace EMS.Domain.DbModels;

/// <summary>Grants a <see cref="Menu"/> to a role defined in <c>um.Roles</c> (referenced by <see cref="RoleId"/> only).</summary>
public class RolePermission
{
    public int Id { get; set; }

    /// <summary>Matches <c>um.Roles.Id</c> in the shared database.</summary>
    public int RoleId { get; set; }

    public int MenuId { get; set; }

    public bool Allowed { get; set; } = true;

    public Menu Menu { get; set; } = null!;
}
