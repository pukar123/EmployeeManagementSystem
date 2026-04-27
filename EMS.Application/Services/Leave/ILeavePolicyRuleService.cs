using EMS.Application.DTOs.Leave;

namespace EMS.Application.Services.Leave;

public interface ILeavePolicyRuleService
{
    Task<IReadOnlyList<LeavePolicyRuleResponseModel>> GetByLeaveTypeAsync(
        int leaveTypeId,
        CancellationToken cancellationToken = default);

    Task<LeavePolicyRuleResponseModel> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LeavePolicyRuleResponseModel> CreateAsync(CreateLeavePolicyRuleRequestModel request, CancellationToken cancellationToken = default);
    Task<LeavePolicyRuleResponseModel> UpdateAsync(int id, UpdateLeavePolicyRuleRequestModel request, CancellationToken cancellationToken = default);
}
