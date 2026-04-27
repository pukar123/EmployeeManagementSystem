namespace EMS.Application.DTOs.Leave;

public class BulkLeaveImportResultResponseModel
{
    public string? ImportKey { get; set; }
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int FailedRows { get; set; }
    public IReadOnlyList<BulkLeaveImportRowResultModel> RowResults { get; set; } = [];
}

public class BulkLeaveImportRowResultModel
{
    public int RowNumber { get; set; }
    public bool Success { get; set; }
    public int? LeaveRequestId { get; set; }
    public string? Error { get; set; }
}
