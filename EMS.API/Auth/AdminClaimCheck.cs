using System.Security.Claims;
using Pukar.Usermanagement.Contracts.Roles;

namespace EMS.API.Auth;

/// <summary>
/// The one test for "this signed-in user is an admin", shared by the admin policies so they cannot drift apart
/// (all except <see cref="AuthorizationPolicyNames.LeaveAdmin"/>, <see cref="AuthorizationPolicyNames.AttendanceAdmin"/> and
/// <see cref="AuthorizationPolicyNames.DocumentAdmin"/>, which use the Application leave, attendance and document admin
/// checks since B06, B07 and B09):
/// a <see cref="ClaimTypes.Role"/> claim equal to <c>Admin</c>, or a <c>roles</c> claim equal to <c>ADMIN</c>
/// (both compared case-insensitively).
/// </summary>
public static class AdminClaimCheck
{
    private const string RoleKeysClaimType = "roles";

    public static bool IsAdmin(ClaimsPrincipal user)
        => user.Claims.Any(c =>
               c.Type == ClaimTypes.Role
               && string.Equals(c.Value, WellKnownRoles.Admin, StringComparison.OrdinalIgnoreCase))
           || user.Claims.Any(c =>
               c.Type == RoleKeysClaimType
               && string.Equals(c.Value, WellKnownRoles.AdminNormalizedName, StringComparison.OrdinalIgnoreCase));
}
