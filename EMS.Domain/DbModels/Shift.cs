using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class Shift
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int EmployeeId { get; set; }
    public int? SiteId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Scheduled;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Employee Employee { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public Site? Site { get; set; }
}
