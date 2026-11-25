namespace EMS.Application.DTOs.Document;

/// <summary>One page of documents. Same JSON shape as the employee directory page.</summary>
public sealed class PagedDocumentResponseModel
{
    public IReadOnlyList<DocumentResponseModel> Items { get; set; } = Array.Empty<DocumentResponseModel>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
