using EMS.Application.Services.Attendance;
using Microsoft.AspNetCore.Authorization;

namespace EMS.API.Auth;

/// <summary>
/// Meets <see cref="AttendanceAdminRequirement"/> by asking the Application layer's single attendance admin check
/// (<see cref="IAttendanceAdminAccessService"/>), so the policy, the per-employee checks, the report scope and the
/// portal flag can never disagree. Registered as scoped because that check reads the request's identity and the database.
/// </summary>
public sealed class AttendanceAdminAuthorizationHandler : AuthorizationHandler<AttendanceAdminRequirement>
{
    private readonly IAttendanceAdminAccessService _attendanceAdminAccess;

    public AttendanceAdminAuthorizationHandler(IAttendanceAdminAccessService attendanceAdminAccess)
    {
        _attendanceAdminAccess = attendanceAdminAccess;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AttendanceAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (await _attendanceAdminAccess.IsAttendanceAdminAsync())
            context.Succeed(requirement);
    }
}
