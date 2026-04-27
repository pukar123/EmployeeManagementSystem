using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Leave;

public class BulkLeaveImportRequestModel
{
    public int OrganizationId { get; set; }
    public string? ImportKey { get; set; }
    public IReadOnlyList<BulkLeaveImportItemModel> Items { get; set; } = [];
}

public class BulkLeaveImportItemModel
{
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public LeaveUnit Unit { get; set; } = LeaveUnit.Days;
    public decimal RequestedAmount { get; set; }
    public string? Reason { get; set; }
}
