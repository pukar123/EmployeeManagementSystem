namespace EMS.Application.DTOs.Employee;

public sealed class EmployeeHistoryResponseModel
{
    public int EmployeeId { get; set; }

    public IReadOnlyList<PositionHistoryItemResponseModel> PositionHistory { get; set; } = Array.Empty<PositionHistoryItemResponseModel>();

    public IReadOnlyList<DepartmentHistoryItemResponseModel> DepartmentHistory { get; set; } = Array.Empty<DepartmentHistoryItemResponseModel>();

    public IReadOnlyList<ManagerHistoryItemResponseModel> ManagerHistory { get; set; } = Array.Empty<ManagerHistoryItemResponseModel>();
}

public sealed class PositionHistoryItemResponseModel
{
    public long Id { get; set; }
    public int? PreviousJobPositionId { get; set; }
    public int? NewJobPositionId { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public string? Reason { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? ChangedByUserName { get; set; }
    public string? ChangedByEmail { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class DepartmentHistoryItemResponseModel
{
    public long Id { get; set; }
    public int? PreviousDepartmentId { get; set; }
    public int? NewDepartmentId { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public string? Reason { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? ChangedByUserName { get; set; }
    public string? ChangedByEmail { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ManagerHistoryItemResponseModel
{
    public long Id { get; set; }
    public int? PreviousManagerId { get; set; }
    public int? NewManagerId { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public string? Reason { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? ChangedByUserName { get; set; }
    public string? ChangedByEmail { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
