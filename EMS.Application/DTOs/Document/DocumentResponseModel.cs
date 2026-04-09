using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Document;

public class DocumentResponseModel
{
    public int Id { get; set; }

    public int? EmployeeId { get; set; }

    public int DocumentTypeId { get; set; }

    public string DocumentTypeName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DocumentFileKind FileKind { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    /// <summary>Relative URL path for download (same host as API).</summary>
    public string StoredRelativePath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
