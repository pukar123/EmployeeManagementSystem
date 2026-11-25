namespace EMS.Application.Services.Documents;

/// <summary>
/// The single definition of "document admin" (B09): a signed-in user who is an admin, or whose roles are granted the
/// <c>documents.admin</c> menu. The <c>employees</c> menu and the <c>employees.view</c> and <c>employees.manage</c>
/// capabilities do not count.
/// Used by <see cref="IDocumentAccessService"/>, the employee portal's documents flag and the API's
/// <c>DocumentAdmin</c> authorisation policy, so all of them always give the same answer.
/// </summary>
public interface IDocumentAdminAccessService
{
    /// <summary>True when the current user is a document admin. Admins are answered without a menu query.</summary>
    Task<bool> IsDocumentAdminAsync(CancellationToken cancellationToken = default);
}
