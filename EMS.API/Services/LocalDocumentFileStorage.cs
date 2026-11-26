using System.Globalization;
using EMS.Application.DTOs.Files;
using EMS.Application.Services.Files;
using EMS.Domain.Enums;

namespace EMS.API.Services;

/// <summary>
/// Checks employee document uploads with the shared upload checker (B10) and stores them in the private store
/// (<see cref="PrivateFileStore"/>). The database holds only the storage key relative to <c>Storage:PrivateRoot</c>;
/// files are never public. The allowed types and the size limit live in <see cref="UploadRules"/>.
/// </summary>
public sealed class LocalDocumentFileStorage
{
    private readonly PrivateFileStore _store;
    private readonly IUploadFileChecker _checker;

    public LocalDocumentFileStorage(PrivateFileStore store, IUploadFileChecker checker)
    {
        _store = store;
        _checker = checker;
    }

    /// <summary>
    /// Checks the file, then saves it under <c>{PrivateRoot}/Employee/{employeeId|unassigned}/Documents</c> and
    /// returns the storage key relative to the private root (for example <c>Employee/5/Documents/{guid}.pdf</c>),
    /// the canonical content type, the file kind and the sanitised display name. A refused file throws
    /// <see cref="UploadRejectedException"/> before anything is written.
    /// </summary>
    public async Task<(string StorageKey, string ContentType, DocumentFileKind Kind, string FileName)> SaveAsync(
        int? employeeId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var content = file.OpenReadStream();
        var checkedFile = _checker.Check(UploadPurpose.EmployeeDocument, content, file.FileName, file.ContentType, file.Length);

        var folder = employeeId is int id ? id.ToString(CultureInfo.InvariantCulture) : "unassigned";
        var key = await _store.SaveAsync(
            $"{PrivateFileStore.EmployeeFolder}/{folder}/Documents", content, checkedFile.Extension, cancellationToken);
        return (key, checkedFile.ContentType, checkedFile.FileKind, checkedFile.DisplayFileName);
    }

    /// <summary>Deletes the stored file for a storage key. Unsafe keys and keys outside the private root are ignored.</summary>
    public void TryDelete(string? storageKey) => _store.TryDelete(storageKey);
}
