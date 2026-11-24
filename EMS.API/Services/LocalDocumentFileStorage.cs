using System.Globalization;
using EMS.Domain.Enums;

namespace EMS.API.Services;

/// <summary>
/// Validates employee document uploads and stores them in the private store (<see cref="PrivateFileStore"/>).
/// The database holds only the storage key relative to <c>Storage:PrivateRoot</c>; files are never public.
/// </summary>
public sealed class LocalDocumentFileStorage
{
    private readonly PrivateFileStore _store;
    private const long MaxBytes = 15 * 1024 * 1024;

    public LocalDocumentFileStorage(PrivateFileStore store)
    {
        _store = store;
    }

    public static bool TryResolveFileKind(string contentType, string fileName, out DocumentFileKind kind)
    {
        kind = DocumentFileKind.None;
        var ct = contentType ?? string.Empty;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ct.Contains("pdf", StringComparison.OrdinalIgnoreCase) || ext == ".pdf")
        {
            kind = DocumentFileKind.Pdf;
            return true;
        }

        if (ct.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase) ||
            ct.Contains("msword", StringComparison.OrdinalIgnoreCase) ||
            ext is ".doc" or ".docx")
        {
            kind = DocumentFileKind.Word;
            return true;
        }

        if (ct.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
            ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp")
        {
            kind = DocumentFileKind.Image;
            return true;
        }

        return false;
    }

    public static void ValidateContentOrThrow(IFormFile file)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("File is empty.");
        if (file.Length > MaxBytes)
            throw new InvalidOperationException($"File exceeds maximum size ({MaxBytes / (1024 * 1024)} MB).");

        if (!TryResolveFileKind(file.ContentType, file.FileName, out _))
            throw new InvalidOperationException("Only PDF, Word (.doc/.docx), or image files are allowed.");
    }

    /// <summary>
    /// Saves under <c>{PrivateRoot}/Employee/{employeeId|unassigned}/Documents</c> and returns the storage key
    /// relative to the private root (for example <c>Employee/5/Documents/{guid}.pdf</c>).
    /// </summary>
    public async Task<(string StorageKey, string ContentType, DocumentFileKind Kind)> SaveAsync(
        int? employeeId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateContentOrThrow(file);
        if (!TryResolveFileKind(file.ContentType, file.FileName, out var kind))
            throw new InvalidOperationException("Could not determine file kind.");

        var folder = employeeId is int id ? id.ToString(CultureInfo.InvariantCulture) : "unassigned";
        var key = await _store.SaveAsync($"{PrivateFileStore.EmployeeFolder}/{folder}/Documents", file, cancellationToken);
        return (key, file.ContentType, kind);
    }

    /// <summary>Deletes the stored file for a storage key. Unsafe keys and keys outside the private root are ignored.</summary>
    public void TryDelete(string? storageKey) => _store.TryDelete(storageKey);
}
