using System.Globalization;
using EMS.Application.DTOs.Files;
using EMS.Application.Services.Files;

namespace EMS.API.Services;

/// <summary>
/// Checks leave attachment uploads with the shared upload checker (B10) and stores them in the private store
/// (<see cref="PrivateFileStore"/>). The database holds only the storage key relative to <c>Storage:PrivateRoot</c>;
/// files are never public. The allowed types and the size limit live in <see cref="UploadRules"/>.
/// </summary>
public sealed class LocalLeaveAttachmentStorage
{
    private readonly PrivateFileStore _store;
    private readonly IUploadFileChecker _checker;

    public LocalLeaveAttachmentStorage(PrivateFileStore store, IUploadFileChecker checker)
    {
        _store = store;
        _checker = checker;
    }

    /// <summary>
    /// Checks the file, then saves it under <c>{PrivateRoot}/Leave/{leaveRequestId}</c> and returns the storage key
    /// relative to the private root (for example <c>Leave/12/{guid}.pdf</c>), the canonical content type, the
    /// sanitised display name and the size. A refused file throws <see cref="UploadRejectedException"/> before
    /// anything is written.
    /// </summary>
    public async Task<(string StorageKey, string ContentType, string FileName, long Size)> SaveAsync(
        int leaveRequestId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var content = file.OpenReadStream();
        var checkedFile = _checker.Check(UploadPurpose.LeaveAttachment, content, file.FileName, file.ContentType, file.Length);

        var folderKey = $"{PrivateFileStore.LeaveFolder}/{leaveRequestId.ToString(CultureInfo.InvariantCulture)}";
        var key = await _store.SaveAsync(folderKey, content, checkedFile.Extension, cancellationToken);
        return (key, checkedFile.ContentType, checkedFile.DisplayFileName, checkedFile.SizeBytes);
    }

    /// <summary>Deletes the stored file for a storage key. Unsafe keys and keys outside the private root are ignored.</summary>
    public void TryDelete(string? storageKey) => _store.TryDelete(storageKey);
}
