namespace EMS.Domain.DbModels;

public class Menu
{
    public int Id { get; set; }

    /// <summary>Stable key for authorization and i18n (e.g. employees, user-management.users).</summary>
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string RoutePath { get; set; } = string.Empty;

    public int? ParentMenuId { get; set; }

    public int SortOrder { get; set; }

    public string? IconKey { get; set; }

    public Menu? ParentMenu { get; set; }

    public ICollection<Menu> ChildMenus { get; set; } = new List<Menu>();

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public ICollection<RoleKeyPermission> RoleKeyPermissions { get; set; } = new List<RoleKeyPermission>();
}
