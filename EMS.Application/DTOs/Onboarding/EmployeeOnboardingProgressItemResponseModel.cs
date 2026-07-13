using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Onboarding;

public class EmployeeOnboardingProgressItemResponseModel
{
    public int TemplateItemId { get; set; }
    public OnboardingChecklistItemCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public int? TaskId { get; set; }
    public TaskWorkflowStatus? Status { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public bool IsOverdue { get; set; }
}
