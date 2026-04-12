namespace EMS.Application.DTOs.Navigation;

public sealed class MenuResponseModel
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string RoutePath { get; set; } = string.Empty;

    public int? ParentMenuId { get; set; }

    public int SortOrder { get; set; }

    public string? IconKey { get; set; }

    public IReadOnlyList<MenuResponseModel> Children { get; set; } = Array.Empty<MenuResponseModel>();
}
