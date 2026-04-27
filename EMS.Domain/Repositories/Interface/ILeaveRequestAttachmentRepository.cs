using EMS.Domain.DbModels;

namespace EMS.Domain.Repositories.Interface;

public interface ILeaveRequestAttachmentRepository : IBaseRepository<LeaveRequestAttachment>
{
    Task<IReadOnlyList<LeaveRequestAttachment>> GetByRequestIdAsync(
        int leaveRequestId,
        CancellationToken cancellationToken = default);
}
