namespace EMS.Domain.DbModels;

/// <summary>
/// Many-to-many link between employees and sites.
/// </summary>
public class EmployeeSite
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int SiteId { get; set; }

    public Employee Employee { get; set; } = null!;

    public Site Site { get; set; } = null!;
}
