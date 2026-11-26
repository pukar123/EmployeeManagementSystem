using System.Globalization;
using EMS.Application.DTOs.Files;
using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Files;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveAttachmentService : ILeaveAttachmentService
{
    private readonly ILeaveRequestAttachmentRepository _attachmentRepository;
    private readonly IBaseRepository<LeaveRequest> _leaveRequestRepository;
    private readonly ILeaveEmployeeAccessService _leaveEmployeeAccess;

    public LeaveAttachmentService(
        ILeaveRequestAttachmentRepository attachmentRepository,
        IBaseRepository<LeaveRequest> leaveRequestRepository,
        ILeaveEmployeeAccessService leaveEmployeeAccess)
    {
        _attachmentRepository = attachmentRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _leaveEmployeeAccess = leaveEmployeeAccess;
    }

    public async Task<IReadOnlyList<LeaveRequestAttachmentResponseModel>> GetByRequestIdAsync(
        int leaveRequestId,
        CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequestId, cancellationToken)
            ?? throw new BusinessRuleException("Leave request was not found.");

        await _leaveEmployeeAccess.EnsureCanAccessLeaveAttachmentsAsync(leaveRequest.EmployeeId, cancellationToken);

        var rows = await _attachmentRepository.GetByRequestIdAsync(leaveRequestId, cancellationToken);
        return rows.Select(ToResponse).ToList();
    }

    public async Task EnsureCanAttachToRequestAsync(int leaveRequestId, CancellationToken cancellationToken = default)
    {
        var request = await _leaveRequestRepository.GetByIdAsync(leaveRequestId, cancellationToken)
            ?? throw new BusinessRuleException("Leave request was not found.");

        await _leaveEmployeeAccess.EnsureCanAccessLeaveAttachmentsAsync(request.EmployeeId, cancellationToken);
    }

    public async Task<LeaveRequestAttachmentResponseModel> AddAsync(
        int leaveRequestId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var request = await _leaveRequestRepository.GetByIdAsync(leaveRequestId, cancellationToken)
            ?? throw new BusinessRuleException("Leave request was not found.");

        // Checked again here so the service stays safe for any caller, not only the upload endpoint.
        await _leaveEmployeeAccess.EnsureCanAccessLeaveAttachmentsAsync(request.EmployeeId, cancellationToken);

        var entity = new LeaveRequestAttachment
        {
            LeaveRequestId = request.Id,
            // Defence in depth (B10): the upload endpoint already passes a sanitised name; cleaning again is cheap
            // and idempotent, and keeps any other caller safe.
            FileName = UploadFileNames.SanitiseDisplayName(
                fileName,
                UploadFileNames.ServerExtensionOfKey(storagePath),
                UploadRules.LeaveAttachment.FallbackBaseName),
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            StoragePath = storagePath,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        await _attachmentRepository.AddAsync(entity, cancellationToken);
        await _attachmentRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<LeaveRequestAttachmentResponseModel> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetAccessibleAttachmentAsync(id, cancellationToken);
        return ToResponse(entity);
    }

    public async Task<StoredFileModel> GetStoredFileAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetAccessibleAttachmentAsync(id, cancellationToken);
        return new StoredFileModel
        {
            StoragePath = entity.StoragePath,
            ContentType = entity.ContentType,
            FileName = entity.FileName,
        };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _attachmentRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(entity.LeaveRequestId, cancellationToken);
        if (leaveRequest is null)
            return false;

        await _leaveEmployeeAccess.EnsureCanAccessLeaveAttachmentsAsync(leaveRequest.EmployeeId, cancellationToken);

        _attachmentRepository.Remove(entity);
        await _attachmentRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Loads an attachment and applies the B06 attachment check (self or leave admin).</summary>
    private async Task<LeaveRequestAttachment> GetAccessibleAttachmentAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _attachmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Leave attachment was not found.");

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(entity.LeaveRequestId, cancellationToken)
            ?? throw new BusinessRuleException("Leave request was not found.");

        await _leaveEmployeeAccess.EnsureCanAccessLeaveAttachmentsAsync(leaveRequest.EmployeeId, cancellationToken);
        return entity;
    }

    private static LeaveRequestAttachmentResponseModel ToResponse(LeaveRequestAttachment entity)
    {
        return new LeaveRequestAttachmentResponseModel
        {
            Id = entity.Id,
            LeaveRequestId = entity.LeaveRequestId,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            FileSizeBytes = entity.FileSizeBytes,
            DownloadUrl = $"/api/LeaveAttachments/{entity.Id.ToString(CultureInfo.InvariantCulture)}/file",
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }
}
