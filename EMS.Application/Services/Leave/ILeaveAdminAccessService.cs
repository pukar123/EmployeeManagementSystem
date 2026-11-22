namespace EMS.Application.Services.Leave;

/// <summary>
/// The single definition of "leave admin" (B06): a signed-in user who is an admin, or whose roles are granted
/// the <c>leave.admin</c> menu. The parent <c>leave</c> menu and the child menus (<c>leave.admin.overview</c>,
/// <c>leave.admin.settings</c>) do not count; they only control navigation.
/// Used by <see cref="ILeaveEmployeeAccessService"/>, <see cref="ILeaveApprovalAccessService"/> and the API's
/// <c>LeaveAdmin</c> authorisation policy, so all three always give the same answer.
/// </summary>
public interface ILeaveAdminAccessService
{
    /// <summary>True when the current user is a leave admin. Admins are answered without a menu query.</summary>
    Task<bool> IsLeaveAdminAsync(CancellationToken cancellationToken = default);
}
