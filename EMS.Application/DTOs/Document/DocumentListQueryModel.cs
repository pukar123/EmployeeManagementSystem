namespace EMS.Application.DTOs.Document;

/// <summary>Query for <c>GET /api/Documents</c> (B09). Always paged.</summary>
public sealed class DocumentListQueryModel
{
    /// <summary>Omitted: every document (document admins only). Given: that employee's documents.</summary>
    public int? EmployeeId { get; set; }

    /// <summary>Optional filter, applied inside the allowed scope.</summary>
    public int? DocumentTypeId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;
}
