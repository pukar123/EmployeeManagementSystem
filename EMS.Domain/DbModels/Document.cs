using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

/// <summary>
/// Stored file metadata. Ownership/association is modeled via link tables (e.g. EmployeeDocument).
/// </summary>
public class Document
{
    public int Id { get; set; }

    public int DocumentTypeId { get; set; }

    /// <summary>Logical name of the document (title).</summary>
    public string Name { get; set; } = string.Empty;

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DocumentFileKind FileKind { get; set; }

    /// <summary>Original upload file name for download display.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>MIME type of the stored file.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Web-relative path under wwwroot (e.g. /uploads/documents/...).</summary>
    public string StoredRelativePath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<EmployeeDocument> EmployeeDocuments { get; set; } = new List<EmployeeDocument>();

    public DocumentType DocumentType { get; set; } = null!;
}
