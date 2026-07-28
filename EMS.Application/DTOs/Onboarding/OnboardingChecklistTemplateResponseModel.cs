namespace EMS.Application.DTOs.Onboarding;

public class OnboardingChecklistTemplateResponseModel
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public IReadOnlyList<OnboardingChecklistTemplateItemResponseModel> Items { get; set; } = Array.Empty<OnboardingChecklistTemplateItemResponseModel>();
}
