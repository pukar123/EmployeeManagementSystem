namespace EMS.Domain.DbModels;

public class OnboardingChecklistTemplate
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<OnboardingChecklistTemplateItem> Items { get; set; } = new List<OnboardingChecklistTemplateItem>();
    public ICollection<EmployeeOnboardingChecklist> EmployeeChecklists { get; set; } = new List<EmployeeOnboardingChecklist>();
}
