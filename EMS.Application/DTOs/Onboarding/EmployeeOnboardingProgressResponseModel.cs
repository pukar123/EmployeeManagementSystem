namespace EMS.Application.DTOs.Onboarding;

public class EmployeeOnboardingProgressResponseModel
{
    public int? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public DateTime? GeneratedAtUtc { get; set; }
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public int OverdueCount { get; set; }
    public int PercentComplete { get; set; }
    public IReadOnlyList<EmployeeOnboardingProgressItemResponseModel> Items { get; set; } = Array.Empty<EmployeeOnboardingProgressItemResponseModel>();
}
