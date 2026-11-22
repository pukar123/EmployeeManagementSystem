using EMS.Application.DTOs.Leave;

namespace EMS.Application.Services.Leave;

public interface ILeaveAttachmentService
{
    Task<IReadOnlyList<LeaveRequestAttachmentResponseModel>> GetByRequestIdAsync(
        int leaveRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks that the leave request exists and that the current user may attach files to it, before any file is
    /// stored. Throws <c>BusinessRuleException</c> ("Leave request was not found." or <see cref="LeaveAccessMessages.Denied"/>).
    /// </summary>
    Task EnsureCanAttachToRequestAsync(int leaveRequestId, CancellationToken cancellationToken = default);

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
