namespace EMS.Domain.DbModels;

/// <summary>
/// Grants a <see cref="Menu"/> to an external role key (normalized role name from identity claims).
/// </summary>
public class RoleKeyPermission
{
    public int Id { get; set; }

    /// <summary>
    /// Normalized role key (for example: ADMIN, HR_MANAGER).
    /// </summary>
    public string RoleKey { get; set; } = string.Empty;

    public int MenuId { get; set; }

    public bool Allowed { get; set; } = true;

    public Menu Menu { get; set; } = null!;
}
