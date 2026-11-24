using Microsoft.Net.Http.Headers;

namespace EMS.API.Services;

/// <summary>Response header values for authorised private file downloads.</summary>
public static class FileDownloadHeaders
{
    public const string FallbackContentType = "application/octet-stream";
    public const string FallbackFileName = "download";

    /// <summary>The stored content type when it parses as a media type; otherwise <c>application/octet-stream</c>.</summary>
    public static string SafeContentType(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType) && MediaTypeHeaderValue.TryParse(contentType, out _)
            ? contentType
            : FallbackContentType;

    /// <summary>The stored file name, or <c>download</c> when it is empty.</summary>
    public static string SafeFileName(string? fileName)
        => string.IsNullOrWhiteSpace(fileName) ? FallbackFileName : fileName;
}
