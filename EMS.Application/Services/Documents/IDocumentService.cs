using EMS.Application.DTOs.Document;
using EMS.Domain.Enums;

namespace EMS.Application.Services.Documents;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentTypeResponseModel>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentResponseModel>> GetAllAsync(int? employeeId, CancellationToken cancellationToken = default);

    Task<DocumentResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

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
