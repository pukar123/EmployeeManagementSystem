using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Files;

/// <summary>
/// Result of a successful upload check. Every value here is chosen by the server; nothing is taken from the upload
/// unchanged. Callers store the file with <see cref="Extension"/> and save <see cref="ContentType"/> and
/// <see cref="DisplayFileName"/> as metadata.
/// </summary>
public sealed class CheckedUploadModel
{
    /// <summary>Server-chosen extension for the checked type, with the dot, in lower case (for example <c>.pdf</c>).</summary>
    public string Extension { get; init; } = string.Empty;

    /// <summary>Canonical content type for the checked type (for example <c>image/jpeg</c>).</summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>Sanitised user file name ending with <see cref="Extension"/>; display metadata only.</summary>
    public string DisplayFileName { get; init; } = string.Empty;

    /// <summary>Document file kind; <see cref="DocumentFileKind.None"/> for organisation logos.</summary>
    public DocumentFileKind FileKind { get; init; }

    public long SizeBytes { get; init; }
}
