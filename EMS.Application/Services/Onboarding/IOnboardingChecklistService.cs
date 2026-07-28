using EMS.Application.DTOs.Onboarding;
using EMS.Domain.DbModels;

namespace EMS.Application.Services.Onboarding;

public interface IOnboardingChecklistService
{
    Task GenerateForEmployeeAsync(
        Employee employee,
        int templateId,
        int? assignedByUserId,
        DateTime anchorDateUtc,
        CancellationToken cancellationToken = default);

    Task<EmployeeOnboardingProgressResponseModel> GetProgressAsync(
        int employeeId,
        CancellationToken cancellationToken = default);
}
