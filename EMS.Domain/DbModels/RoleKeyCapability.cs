namespace EMS.Domain.DbModels;

/// <summary>
/// Grants a named capability to an external role key (normalized role name from identity claims).
/// </summary>
public class RoleKeyCapability
{
    public int Id { get; set; }

    /// <summary>
    /// Normalized role key (for example: ADMIN, HR_MANAGER).
    /// </summary>
    public string RoleKey { get; set; } = string.Empty;

    /// <summary>
    /// Stable capability key (for example: employees.view, employees.manage).
    /// </summary>
    public string CapabilityKey { get; set; } = string.Empty;

    public bool Allowed { get; set; } = true;
}
