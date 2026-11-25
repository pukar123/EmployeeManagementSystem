using Microsoft.AspNetCore.Authorization;

namespace EMS.API.Auth;

/// <summary>
/// Requirement behind the <see cref="AuthorizationPolicyNames.DocumentAdmin"/> policy: the caller is a document admin
/// (an admin, or a role granted the <c>documents.admin</c> menu). Met by <see cref="DocumentAdminAuthorizationHandler"/>.
/// </summary>
public sealed class DocumentAdminRequirement : IAuthorizationRequirement
{
}
