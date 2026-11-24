using System.Globalization;

namespace EMS.API.Services;

/// <summary>
/// Validates leave attachment uploads and stores them in the private store (<see cref="PrivateFileStore"/>).
/// The database holds only the storage key relative to <c>Storage:PrivateRoot</c>; files are never public.
/// </summary>
public sealed class LocalLeaveAttachmentStorage
{
    private readonly PrivateFileStore _store;
    private const long MaxBytes = 15 * 1024 * 1024;

    public LocalLeaveAttachmentStorage(PrivateFileStore store)
    {
        _store = store;
    }

    public static void ValidateContentOrThrow(IFormFile file)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("File is empty.");
        if (file.Length > MaxBytes)
            throw new InvalidOperationException($"File exceeds maximum size ({MaxBytes / (1024 * 1024)} MB).");
    }

    /// <summary>
    /// Saves under <c>{PrivateRoot}/Leave/{leaveRequestId}</c> and returns the storage key relative to the private
    /// root (for example <c>Leave/12/{guid}.pdf</c>).
    /// </summary>
    public async Task<(string StorageKey, string ContentType, string FileName, long Size)> SaveAsync(
        int leaveRequestId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateContentOrThrow(file);

        var folderKey = $"{PrivateFileStore.LeaveFolder}/{leaveRequestId.ToString(CultureInfo.InvariantCulture)}";
        var key = await _store.SaveAsync(folderKey, file, cancellationToken);
        return (key, file.ContentType, file.FileName, file.Length);
    }

    /// <summary>Deletes the stored file for a storage key. Unsafe keys and keys outside the private root are ignored.</summary>
    public void TryDelete(string? storageKey) => _store.TryDelete(storageKey);
}
