using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Employee;

public sealed class EmployeeProfileResponseModel
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime DateJoined { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime? RetentionUntilUtc { get; set; }
    public string? ArchiveReason { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? JobPositionId { get; set; }
    public string? JobPositionTitle { get; set; }
    public string? JobPositionCode { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmployeeNumber { get; set; }
    public int? LocationId { get; set; }
    public string? LocationLabel { get; set; }
    public string? PrimarySiteName { get; set; }
    public IReadOnlyList<string> SiteNames { get; set; } = Array.Empty<string>();
    public bool HasLinkedLogin { get; set; }
    public int? LinkedUserId { get; set; }
    public string? LinkedUserEmail { get; set; }
    public bool? LinkedLoginIsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
