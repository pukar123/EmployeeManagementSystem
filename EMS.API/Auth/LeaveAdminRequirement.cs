using Microsoft.AspNetCore.Authorization;

namespace EMS.API.Auth;

/// <summary>
/// Requirement behind the <see cref="AuthorizationPolicyNames.LeaveAdmin"/> policy: the caller is a leave admin
/// (an admin, or a role granted the <c>leave.admin</c> menu). Met by <see cref="LeaveAdminAuthorizationHandler"/>.
/// </summary>
public sealed class LeaveAdminRequirement : IAuthorizationRequirement
{
}
