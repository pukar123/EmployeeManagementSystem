using EMS.Application.DTOs.Leave;

namespace EMS.Application.Services.Leave;

public interface ILeaveAttachmentService
{
    Task<IReadOnlyList<LeaveRequestAttachmentResponseModel>> GetByRequestIdAsync(
        int leaveRequestId,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestAttachmentResponseModel> AddAsync(
        int leaveRequestId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string storagePath,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestAttachmentResponseModel> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
