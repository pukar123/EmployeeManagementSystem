using EMS.Domain.DbModels;

namespace EMS.Domain.Repositories.Interface;

public interface ILeaveTypeRepository : IBaseRepository<LeaveType>
{
    Task<IReadOnlyList<LeaveType>> GetActiveByOrganizationAsync(
        int organizationId,
        CancellationToken cancellationToken = default);
}
