using EMS.Domain.DbModels;

namespace EMS.Domain.Repositories.Interface;

public interface ILeavePolicyRuleRepository : IBaseRepository<LeavePolicyRule>
{
    Task<IReadOnlyList<LeavePolicyRule>> GetByLeaveTypeAsync(
        int leaveTypeId,
        CancellationToken cancellationToken = default);
}
