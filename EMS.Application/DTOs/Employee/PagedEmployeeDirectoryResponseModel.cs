namespace EMS.Application.DTOs.Employee;

public sealed class PagedEmployeeDirectoryResponseModel
{
    public IReadOnlyList<EmployeeDirectoryItemResponseModel> Items { get; set; } = Array.Empty<EmployeeDirectoryItemResponseModel>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
