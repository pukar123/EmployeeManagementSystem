namespace EMS.Domain.DbModels;

public class EmployeeRetentionPolicy
{
    public int Id { get; set; }

    public int? OrganizationId { get; set; }

    public int RetentionDays { get; set; } = 3650;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Organization? Organization { get; set; }
}
