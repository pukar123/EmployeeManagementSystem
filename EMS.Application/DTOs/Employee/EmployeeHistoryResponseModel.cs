namespace EMS.Application.DTOs.Employee;

public sealed class EmployeeHistoryResponseModel
{
    public int EmployeeId { get; set; }

    public IReadOnlyList<PositionHistoryItemResponseModel> PositionHistory { get; set; } = Array.Empty<PositionHistoryItemResponseModel>();

    public IReadOnlyList<DepartmentHistoryItemResponseModel> DepartmentHistory { get; set; } = Array.Empty<DepartmentHistoryItemResponseModel>();

    public IReadOnlyList<ManagerHistoryItemResponseModel> ManagerHistory { get; set; } = Array.Empty<ManagerHistoryItemResponseModel>();

    public IReadOnlyList<EmploymentStatusHistoryItemResponseModel> EmploymentStatusHistory { get; set; } = Array.Empty<EmploymentStatusHistoryItemResponseModel>();
}

public sealed class EmploymentStatusHistoryItemResponseModel
{
    public long Id { get; set; }
    public EMS.Domain.Enums.EmploymentStatus? PreviousStatus { get; set; }
    public EMS.Domain.Enums.EmploymentStatus NewStatus { get; set; }
    public DateTime EffectiveDateUtc { get; set; }
    public string? Reason { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? ChangedByUserName { get; set; }
    public string? ChangedByEmail { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class PositionHistoryItemResponseModel
{
    public long Id { get; set; }
    public int? PreviousJobPositionId { get; set; }
    public string? PreviousJobPositionTitle { get; set; }
    public int? NewJobPositionId { get; set; }
    public string? NewJobPositionTitle { get; set; }
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
    public string? PreviousDepartmentName { get; set; }
    public int? NewDepartmentId { get; set; }
    public string? NewDepartmentName { get; set; }
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
    public string? PreviousManagerName { get; set; }
    public string? PreviousManagerEmployeeNumber { get; set; }
    public int? NewManagerId { get; set; }
    public string? NewManagerName { get; set; }
    public string? NewManagerEmployeeNumber { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public string? Reason { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? ChangedByUserName { get; set; }
    public string? ChangedByEmail { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
