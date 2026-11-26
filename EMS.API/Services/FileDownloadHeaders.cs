using EMS.Application.Services.Files;
using Microsoft.Net.Http.Headers;

namespace EMS.API.Services;

/// <summary>
/// Response header values for authorised private file downloads (B08, B10). The content type comes only from an
/// allow-list and the file name is cleaned with the same rules as uploads, so rows saved before B10 are safe too.
/// </summary>
public static class FileDownloadHeaders
{
    public const string FallbackContentType = "application/octet-stream";
    public const string FallbackFileName = "download";
    public const string NoSniffHeader = "X-Content-Type-Options";
    public const string NoSniffValue = "nosniff";

    /// <summary>
    /// Content types a download may be served with. <c>application/msword</c>, GIF, WebP and BMP exist only for rows
    /// saved before B10; new uploads are PDF, Word (.docx), PNG or JPEG.
    /// </summary>
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.Ordinal)
    {
        "application/pdf",
        UploadRules.DocxContentType,
        "application/msword",
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp",
        "image/bmp",
    };

    /// <summary>
    /// The stored media type (lower case, without parameters) when it is on the download allow-list; anything else,
    /// including <c>text/html</c>, <c>text/plain</c>, <c>image/svg+xml</c> or an unparseable value, is
    /// <c>application/octet-stream</c>.
    /// </summary>
    public static string SafeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType) || !MediaTypeHeaderValue.TryParse(contentType, out var parsed))
            return FallbackContentType;

        var mediaType = parsed.MediaType.Value?.ToLowerInvariant();
        return mediaType is not null && AllowedContentTypes.Contains(mediaType) ? mediaType : FallbackContentType;
    }

    /// <summary>
    /// The stored file name cleaned with <see cref="UploadFileNames.SanitiseDisplayName"/>, ending with the stored
    /// key's extension when that is a plain <c>.xxx</c>/<c>.xxxx</c>; <c>download</c> when nothing is left.
    /// </summary>
    public static string SafeFileName(string? fileName, string? storageKey)
        => UploadFileNames.SanitiseDisplayName(fileName, UploadFileNames.ServerExtensionOfKey(storageKey), FallbackFileName);
}
