using EMS.Domain.DbModels;

namespace EMS.Application.Services.Leave;

/// <summary>
/// Decides whether the current user may approve or reject a specific leave request.
/// Allowed: the employee's current direct manager (manager.team permission) or a leave admin as defined by
/// <see cref="ILeaveAdminAccessService"/> (an admin, or a role granted the leave.admin menu).
/// The reviewer must be linked to an employee in the request's organisation, and is never the employee themselves.
/// </summary>
public interface ILeaveApprovalAccessService
{
    /// <summary>
    /// Returns the reviewer's employee id (recorded as <c>ReviewedByEmployeeId</c>), or throws
    /// <c>BusinessRuleException</c> with one of the <see cref="LeaveApprovalMessages"/> access messages.
    /// </summary>
    Task<int> EnsureCanDecideAsync(LeaveRequest request, CancellationToken cancellationToken = default);
}
