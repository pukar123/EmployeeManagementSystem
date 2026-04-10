using EMS.Application.DTOs.Document;
using EMS.Domain.DbModels;

namespace EMS.Application.Mapping;

internal static class DocumentMapper
{
    public static DocumentResponseModel ToResponse(Document entity)
    {
        return new DocumentResponseModel
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeDocuments
                .Where(ed => ed.IsActive && !ed.IsDeleted)
                .Select(ed => (int?)ed.EmployeeId)
                .FirstOrDefault(),
            DocumentTypeId = entity.DocumentTypeId,
            DocumentTypeName = entity.DocumentType?.Name ?? string.Empty,
            Name = entity.Name,
            IssueDate = entity.IssueDate,
            ExpiryDate = entity.ExpiryDate,
            FileKind = entity.FileKind,
            OriginalFileName = entity.OriginalFileName,
            ContentType = entity.ContentType,
            StoredRelativePath = entity.StoredRelativePath,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }
}
