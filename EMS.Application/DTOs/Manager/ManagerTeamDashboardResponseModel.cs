namespace EMS.Application.DTOs.Manager;

public sealed class ManagerTeamDashboardResponseModel
{
    public int ManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public string ManagerEmployeeNumber { get; set; } = string.Empty;
    public bool AllowsManagerSelection { get; set; }
    public ManagerTeamSummaryResponseModel Summary { get; set; } = new();
    public IReadOnlyList<ManagerTeamMemberResponseModel> Items { get; set; } = Array.Empty<ManagerTeamMemberResponseModel>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
