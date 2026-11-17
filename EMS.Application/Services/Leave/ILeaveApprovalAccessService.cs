using EMS.Domain.DbModels;

namespace EMS.Application.Services.Leave;

/// <summary>
/// Decides whether the current user may approve or reject a specific leave request.
/// Allowed: the employee's current direct manager (manager.team permission) or a leave administrator
/// (ADMIN/ADMINISTRATOR role or leave.admin menu permission). Never the employee themselves.
/// </summary>
public interface ILeaveApprovalAccessService
{
    /// <summary>
    /// Returns the reviewer's employee id (recorded as <c>ReviewedByEmployeeId</c>), or throws
    /// <c>BusinessRuleException</c> with one of the <see cref="LeaveApprovalMessages"/> access messages.
    /// </summary>
    Task<int> EnsureCanDecideAsync(LeaveRequest request, CancellationToken cancellationToken = default);
}
