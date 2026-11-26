using EMS.Application.DTOs.Files;

namespace EMS.Application.Services.Files;

/// <summary>
/// The shared upload checker (B10). A file is accepted only when its extension, its declared content type and its
/// content signature all match the same allowed type for the purpose, and its size is within the purpose's limit.
/// </summary>
public interface IUploadFileChecker
{
    /// <summary>
    /// Checks <paramref name="content"/> (which must be seekable) and returns the server-chosen extension, content
    /// type, sanitised display name and kind. Throws <see cref="UploadRejectedException"/> on any failure. The stream
    /// is left at position 0.
    /// </summary>
    CheckedUploadModel Check(UploadPurpose purpose, Stream content, string? fileName, string? declaredContentType, long length);
}
