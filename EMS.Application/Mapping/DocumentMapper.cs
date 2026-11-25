using System.Globalization;
using EMS.Application.DTOs.Document;
using EMS.Domain.DbModels;

namespace EMS.Application.Mapping;

internal static class DocumentMapper
{
    /// <summary>
    /// Maps a document loaded with its type and its links (and each link's employee, for the display name).
    /// The owner is the employee on the active, non-deleted link.
    /// </summary>
    public static DocumentResponseModel ToResponse(Document entity)
    {
        var owner = entity.EmployeeDocuments.FirstOrDefault(ed => ed.IsActive && !ed.IsDeleted);
        return new DocumentResponseModel
        {
            Id = entity.Id,
            EmployeeId = owner?.EmployeeId,
            EmployeeDisplayName = owner?.Employee is null ? null : DisplayName(owner.Employee.FirstName, owner.Employee.LastName),
            DocumentTypeId = entity.DocumentTypeId,
            DocumentTypeName = entity.DocumentType?.Name ?? string.Empty,
            Name = entity.Name,
            IssueDate = entity.IssueDate,
            ExpiryDate = entity.ExpiryDate,
            FileKind = entity.FileKind,
            OriginalFileName = entity.OriginalFileName,
            ContentType = entity.ContentType,
            DownloadUrl = DownloadUrlFor(entity.Id),
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }

    /// <summary>"First Last", trimmed.</summary>
    public static string DisplayName(string? firstName, string? lastName)
        => $"{firstName} {lastName}".Trim();

    /// <summary>The authorised download endpoint for a document, relative to the API origin.</summary>
    public static string DownloadUrlFor(int documentId)
        => $"/api/Documents/{documentId.ToString(CultureInfo.InvariantCulture)}/file";
}
