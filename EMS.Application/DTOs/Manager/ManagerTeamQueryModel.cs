using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Manager;

public sealed class ManagerTeamQueryModel
{
    public int OrganizationId { get; set; }
    public int? ManagerId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    public EmploymentStatus? EmploymentStatus { get; set; }
    public int? DepartmentId { get; set; }
    public string SortBy { get; set; } = "name";
    public string SortDirection { get; set; } = "asc";
}
