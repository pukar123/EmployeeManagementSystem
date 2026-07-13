namespace EMS.Application.DTOs.Onboarding;

public class CreateOnboardingChecklistTemplateRequestModel
{
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public IReadOnlyList<OnboardingChecklistTemplateItemRequestModel> Items { get; set; } = Array.Empty<OnboardingChecklistTemplateItemRequestModel>();
}
