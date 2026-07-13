using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Employee;

public class CreateEmployeeRequestModel
{
    public int OrganizationId { get; set; }
    public int? DepartmentId { get; set; }
    public int? LocationId { get; set; }
    public int? ManagerId { get; set; }
    public int? JobPositionId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime DateJoined { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    public int? OnboardingTemplateId { get; set; }
    public bool GenerateOnboardingTasks { get; set; }

    public DateTime? PositionEffectiveFromUtc { get; set; }
    public string? PositionChangeReason { get; set; }

    public DateTime? DepartmentEffectiveFromUtc { get; set; }
    public string? DepartmentChangeReason { get; set; }

    public DateTime? ManagerEffectiveFromUtc { get; set; }
    public string? ManagerChangeReason { get; set; }
}
