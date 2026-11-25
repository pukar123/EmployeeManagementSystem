namespace EMS.Application.Services.Documents;

/// <summary>
/// Who may see and change employee documents (B09). Every denial throws
/// <c>BusinessRuleException(DocumentAccessMessages.Denied)</c>.
/// <list type="bullet">
/// <item>Read one employee's documents: the employee themselves, their current direct manager (with the
/// <c>manager.team</c> menu) or a document admin.</item>
/// <item>Read a document with no owner, list every document, and upload, change or delete: document admins only.</item>
/// <item>Unknown ids are refused for everyone but document admins, so ids cannot be probed.</item>
/// </list>
/// </summary>
public interface IDocumentAccessService
{
    /// <summary>Throws Denied unless the current user is a document admin.</summary>
    Task EnsureIsDocumentAdminAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws Denied unless the current user may read the documents of <paramref name="employeeId"/>. A document
    /// admin may ask for an employee that does not exist (the list is then empty); anyone else is refused.
    /// </summary>
    Task EnsureCanViewEmployeeDocumentsAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks that the current user may read (get or download) one document, before anything about its file is
    /// resolved. The owner is the employee on the document's active, non-deleted link; a document with no such link
    /// is for document admins only. Returns false only when the document does not exist and the caller is a
    /// document admin (so the API answers 404); throws Denied in every refused case, including an unknown id for
    /// anyone else.
    /// </summary>
    Task<bool> EnsureCanViewDocumentAsync(int documentId, CancellationToken cancellationToken = default);
}
