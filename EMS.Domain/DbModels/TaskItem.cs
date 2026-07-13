using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class TaskItem
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int? OrganizationId { get; set; }
    public int? AssignedByUserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskWorkflowStatus Status { get; set; } = TaskWorkflowStatus.Assigned;
    public TaskPriority? Priority { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? StartAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public int? OnboardingTemplateItemId { get; set; }
    public int? EmployeeOnboardingChecklistId { get; set; }

    public Employee Employee { get; set; } = null!;
    public OnboardingChecklistTemplateItem? OnboardingTemplateItem { get; set; }
    public EmployeeOnboardingChecklist? EmployeeOnboardingChecklist { get; set; }
}
