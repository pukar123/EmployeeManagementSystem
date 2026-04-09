using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

/// <summary>
/// Stored employee-related file metadata. <see cref="EmployeeId"/> is nullable so documents can be
/// linked to other entities later without schema churn.
/// </summary>
public class Document
{
    public int Id { get; set; }

    /// <summary>Optional link to an employee; null when not (yet) associated.</summary>
    public int? EmployeeId { get; set; }

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

    public Employee? Employee { get; set; }

    public DocumentType DocumentType { get; set; } = null!;
}
