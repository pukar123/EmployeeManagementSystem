using EMS.Application.Services.Leave;
using Microsoft.AspNetCore.Authorization;

namespace EMS.API.Auth;

/// <summary>
/// Meets <see cref="LeaveAdminRequirement"/> by asking the Application layer's single leave admin check
/// (<see cref="ILeaveAdminAccessService"/>), so the policy, the approval rules and the employee leave access rules
/// can never disagree. Registered as scoped because that check reads the request's identity and the database.
/// </summary>
public sealed class LeaveAdminAuthorizationHandler : AuthorizationHandler<LeaveAdminRequirement>
{
    private readonly ILeaveAdminAccessService _leaveAdminAccess;

    public LeaveAdminAuthorizationHandler(ILeaveAdminAccessService leaveAdminAccess)
    {
        _leaveAdminAccess = leaveAdminAccess;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        LeaveAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (await _leaveAdminAccess.IsLeaveAdminAsync())
            context.Succeed(requirement);
    }
}
