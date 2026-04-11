namespace EMS.Domain.DbModels;

public class Site
{
    public int SiteId { get; set; }

    public string SiteName { get; set; } = string.Empty;

    public string? SiteDescription { get; set; }

    public string SiteLocation { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public ICollection<EmployeeSite> EmployeeSites { get; set; } = new List<EmployeeSite>();
}
