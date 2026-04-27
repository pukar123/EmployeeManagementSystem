using EMS.Application.DTOs.Leave;

namespace EMS.Application.Services.Leave;

public interface ILeaveBalanceService
{
    Task<IReadOnlyList<LeaveBalanceResponseModel>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<LeaveBalanceResponseModel> GetByEmployeeAndTypeAsync(
        int employeeId,
        int leaveTypeId,
        CancellationToken cancellationToken = default);
}
