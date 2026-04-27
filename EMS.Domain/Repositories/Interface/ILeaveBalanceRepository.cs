using EMS.Domain.DbModels;

namespace EMS.Domain.Repositories.Interface;

public interface ILeaveBalanceRepository : IBaseRepository<LeaveBalance>
{
    Task<LeaveBalance?> GetByEmployeeAndTypeAsync(
        int employeeId,
        int leaveTypeId,
        CancellationToken cancellationToken = default);
}
