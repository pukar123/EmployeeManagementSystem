using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class OnboardingChecklistTemplateItem
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public OnboardingChecklistItemCategory Category { get; set; }
    public int SortOrder { get; set; }
    public int? DefaultDueDaysFromStart { get; set; }
    public TaskPriority? DefaultPriority { get; set; }
    public bool IsRequired { get; set; } = true;

    public OnboardingChecklistTemplate Template { get; set; } = null!;
    public ICollection<TaskItem> GeneratedTasks { get; set; } = new List<TaskItem>();
}
