using EMS.Application.DTOs.Document;
using EMS.Application.DTOs.Files;
using EMS.Domain.Enums;

namespace EMS.Application.Services.Documents;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentTypeResponseModel>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// One page of documents (B09), newest change first. With an employee id: that employee's documents, for self,
    /// their current direct manager or a document admin. Without: every document, including those with no owner,
    /// for document admins only. Page must be 1 or more and the page size 1 to 100 (otherwise a business-rule error).
    /// Throws <c>DocumentAccessMessages.Denied</c> when refused, before any document is read.
    /// </summary>
    Task<PagedDocumentResponseModel> GetPageAsync(DocumentListQueryModel query, CancellationToken cancellationToken = default);

    /// <summary>
    /// One document, or null when it does not exist and the caller is a document admin. Access as
    /// <see cref="IDocumentAccessService.EnsureCanViewDocumentAsync"/>.
    /// </summary>
    Task<DocumentResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The stored file of a document (storage key, content type and original file name) for the API to stream or
    /// delete. Never return this model to a client. Access-checked (B09) before the file is looked at, the same as
    /// <see cref="GetByIdAsync"/>: null only when the document does not exist and the caller is a document admin;
    /// throws <c>DocumentAccessMessages.Denied</c> otherwise when refused.
    /// </summary>
    Task<StoredFileModel?> GetStoredFileAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Document admins only (throws Denied otherwise). The API's DocumentAdmin policy has already refused others before any file is saved.</summary>
    /// <param name="storedRelativePath">Storage key relative to <c>Storage:PrivateRoot</c>, e.g. <c>Employee/5/Documents/{guid}.pdf</c>.</param>
    Task<DocumentResponseModel> CreateAsync(
        int? employeeId,
        int documentTypeId,
        string name,
        DateTime? issueDate,
        DateTime? expiryDate,
        string originalFileName,
        string contentType,
        string storedRelativePath,
        DocumentFileKind fileKind,
        CancellationToken cancellationToken = default);

    /// <summary>Document admins only (throws Denied otherwise). Null when the document does not exist.</summary>
    Task<DocumentResponseModel?> UpdateAsync(int id, UpdateDocumentRequestModel request, CancellationToken cancellationToken = default);

    /// <summary>Document admins only (throws Denied otherwise). False when the document does not exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
