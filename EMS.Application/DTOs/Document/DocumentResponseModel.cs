using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Document;

public class DocumentResponseModel
{
    public int Id { get; set; }

    public int? EmployeeId { get; set; }

    /// <summary>"First Last" of the owning employee, or null when the document has no owner.</summary>
    public string? EmployeeDisplayName { get; set; }

    public int DocumentTypeId { get; set; }

    public string DocumentTypeName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DocumentFileKind FileKind { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Authorised download endpoint relative to the API origin (<c>/api/Documents/{id}/file</c>). The storage path is
    /// never returned: clients download through this endpoint with a signed-in request.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
