using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Employee;

public sealed class EmployeeDirectoryItemResponseModel
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public EmploymentStatus EmploymentStatus { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? JobPositionId { get; set; }
    public string? JobPositionTitle { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmployeeNumber { get; set; }
    public string? PrimarySiteName { get; set; }
    public bool HasLinkedLogin { get; set; }
    public bool? LinkedLoginIsActive { get; set; }
}
