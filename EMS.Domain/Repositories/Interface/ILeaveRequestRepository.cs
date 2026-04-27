using EMS.Domain.DbModels;
using EMS.Domain.Enums;

namespace EMS.Domain.Repositories.Interface;

public interface ILeaveRequestRepository : IBaseRepository<LeaveRequest>
{
    Task<IReadOnlyList<LeaveRequest>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequest>> GetByStatusAsync(
        int organizationId,
        LeaveRequestStatus status,
        CancellationToken cancellationToken = default);
}
