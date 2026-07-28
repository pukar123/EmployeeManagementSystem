namespace EMS.Domain.DbModels;

public class EmployeeOnboardingChecklist
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int TemplateId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public int? GeneratedByUserId { get; set; }

    public Employee Employee { get; set; } = null!;
    public OnboardingChecklistTemplate Template { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
