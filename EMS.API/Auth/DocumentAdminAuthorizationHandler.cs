using EMS.Application.Services.Documents;
using Microsoft.AspNetCore.Authorization;

namespace EMS.API.Auth;

/// <summary>
/// Meets <see cref="DocumentAdminRequirement"/> by asking the Application layer's single document admin check
/// (<see cref="IDocumentAdminAccessService"/>), so the policy, the document access checks and the portal flag can
/// never disagree. Registered as scoped because that check reads the request's identity and the database.
/// The policy runs before the action, so a refused upload is stopped before any file is saved.
/// </summary>
public sealed class DocumentAdminAuthorizationHandler : AuthorizationHandler<DocumentAdminRequirement>
{
    private readonly IDocumentAdminAccessService _documentAdminAccess;

    public DocumentAdminAuthorizationHandler(IDocumentAdminAccessService documentAdminAccess)
    {
        _documentAdminAccess = documentAdminAccess;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (await _documentAdminAccess.IsDocumentAdminAsync())
            context.Succeed(requirement);
    }
}
