using EMS.Application.DTOs.Leave;
using EMS.Application.DTOs.Shift;

namespace EMS.Application.DTOs.EmployeePortal;

public class EmployeePortalResponseModel
{
    public int EmployeeId { get; set; }
    public int OrganizationId { get; set; }
    public ShiftResponseModel? NearestUpcomingShift { get; set; }
    public IReadOnlyList<ShiftResponseModel> TopThreeUpcomingShifts { get; set; } = Array.Empty<ShiftResponseModel>();
    public IReadOnlyList<ShiftResponseModel> AllUpcomingShifts { get; set; } = Array.Empty<ShiftResponseModel>();
    public IReadOnlyList<LeaveBalanceResponseModel> LeaveBalances { get; set; } = Array.Empty<LeaveBalanceResponseModel>();
    public IReadOnlyList<LeaveRequestResponseModel> LeaveRequests { get; set; } = Array.Empty<LeaveRequestResponseModel>();
}
