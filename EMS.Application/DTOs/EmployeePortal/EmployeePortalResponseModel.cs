using EMS.Application.DTOs.Leave;

namespace EMS.Application.DTOs.EmployeePortal;

public class EmployeePortalResponseModel
{
    public bool HasLinkedEmployeeProfile { get; set; }
    public int? EmployeeId { get; set; }
    public int? OrganizationId { get; set; }
    public IReadOnlyList<PortalScheduleEntryResponseModel> Schedule { get; set; } = Array.Empty<PortalScheduleEntryResponseModel>();
    public IReadOnlyList<LeaveBalanceResponseModel> LeaveBalances { get; set; } = Array.Empty<LeaveBalanceResponseModel>();
    public IReadOnlyList<LeaveRequestResponseModel> LeaveRequests { get; set; } = Array.Empty<LeaveRequestResponseModel>();
}
