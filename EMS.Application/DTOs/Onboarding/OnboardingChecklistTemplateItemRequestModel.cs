using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Onboarding;

public class OnboardingChecklistTemplateItemRequestModel
{
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public OnboardingChecklistItemCategory Category { get; set; }
    public int SortOrder { get; set; }
    public int? DefaultDueDaysFromStart { get; set; }
    public TaskPriority? DefaultPriority { get; set; }
    public bool IsRequired { get; set; } = true;
}
