namespace EMS.Application.DTOs.Site;

public class SiteResponseModel
{
    public int SiteId { get; set; }

    public string SiteName { get; set; } = string.Empty;

    public string? SiteDescription { get; set; }

    public string SiteLocation { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }
}
