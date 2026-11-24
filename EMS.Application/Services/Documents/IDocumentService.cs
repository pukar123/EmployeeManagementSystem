using EMS.Application.DTOs.Document;
using EMS.Application.DTOs.Files;
using EMS.Domain.Enums;

namespace EMS.Application.Services.Documents;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentTypeResponseModel>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentResponseModel>> GetAllAsync(int? employeeId, CancellationToken cancellationToken = default);

    Task<DocumentResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The stored file of a document (storage key, content type and original file name) for the API to stream or
    /// delete, or null when the document does not exist. Never return this model to a client.
    /// Adds no access check beyond "signed in" (per-employee document access is B09).
    /// </summary>
    Task<StoredFileModel?> GetStoredFileAsync(int id, CancellationToken cancellationToken = default);

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

    Task<DocumentResponseModel?> UpdateAsync(int id, UpdateDocumentRequestModel request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
