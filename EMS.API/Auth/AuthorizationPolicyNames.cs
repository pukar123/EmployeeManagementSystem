namespace EMS.API.Auth;

/// <summary>
/// Names of the authorisation policies registered in <see cref="UserManagementJwtExtensions"/>.
/// Controllers use these constants with <c>[Authorize(Policy = ...)]</c> instead of string literals.
/// Each admin-area policy is its own name so that a finer role can be given one area later
/// without touching the controllers. All of them mean "is an admin" except <see cref="LeaveAdmin"/>,
/// which since B06 means "is a leave admin".
/// </summary>
public static class AuthorizationPolicyNames
{
    /// <summary>General admin-only access (role menu and capability management, authorisation tools).</summary>
    public const string AdminAccess = "AdminAccess";

    /// <summary>Changing which roles a job position grants (and so which roles its employees hold).</summary>
    public const string RoleAdmin = "RoleAdmin";

    /// <summary>Creating, changing and deleting organisations, departments, sites, locations and job positions.</summary>
    public const string OrganizationAdmin = "OrganizationAdmin";

    /// <summary>Creating, changing and deleting menus.</summary>
    public const string MenuAdmin = "MenuAdmin";

    /// <summary>
    /// Leave types, leave policy rules, accrual and year-reset runs, bulk leave import and the leave admin summary.
    /// Means "is a leave admin": an admin, or a role granted the <c>leave.admin</c> menu, decided by the Application
    /// layer's <c>ILeaveAdminAccessService</c> through <see cref="LeaveAdminAuthorizationHandler"/>.
    /// </summary>
    public const string LeaveAdmin = "LeaveAdmin";
}
