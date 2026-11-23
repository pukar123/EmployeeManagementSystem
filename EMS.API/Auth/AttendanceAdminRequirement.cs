using Microsoft.AspNetCore.Authorization;

namespace EMS.API.Auth;

/// <summary>
/// Requirement behind the <see cref="AuthorizationPolicyNames.AttendanceAdmin"/> policy: the caller is an attendance
/// admin (an admin, or a role granted the <c>attendance.admin</c> menu). Met by
/// <see cref="AttendanceAdminAuthorizationHandler"/>.
/// </summary>
public sealed class AttendanceAdminRequirement : IAuthorizationRequirement
{
}
