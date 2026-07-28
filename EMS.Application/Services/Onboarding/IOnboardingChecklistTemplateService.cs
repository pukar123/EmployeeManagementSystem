using EMS.Application.DTOs.Onboarding;

namespace EMS.Application.Services.Onboarding;

public interface IOnboardingChecklistTemplateService
{
    Task<IReadOnlyList<OnboardingChecklistTemplateResponseModel>> GetByOrganizationAsync(
        int organizationId,
        CancellationToken cancellationToken = default);

    Task<OnboardingChecklistTemplateResponseModel> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<OnboardingChecklistTemplateResponseModel> CreateAsync(
        CreateOnboardingChecklistTemplateRequestModel request,
        CancellationToken cancellationToken = default);

    Task<OnboardingChecklistTemplateResponseModel> UpdateAsync(
        int id,
        UpdateOnboardingChecklistTemplateRequestModel request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
