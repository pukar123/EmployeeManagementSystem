using EMS.Application.DTOs.Leave;
using EMS.Application.DTOs.Manager;

namespace EMS.Application.Services.Leave;

/// <summary>
/// Manager/HR leave approval workflow: the manager's pending queue, and approve/reject decisions.
/// Approval consumes leave balance (UsedAmount) in the same save as the status change.
/// </summary>
public interface ILeaveApprovalService
{
    Task<IReadOnlyList<ManagerLeaveApprovalItemResponseModel>> GetManagerQueueAsync(
        ManagerLeaveApprovalQueryModel query,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestResponseModel> ApproveAsync(
        int id,
        ApproveLeaveRequestRequestModel request,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestResponseModel> RejectAsync(
        int id,
        RejectLeaveRequestRequestModel request,
        CancellationToken cancellationToken = default);
}
