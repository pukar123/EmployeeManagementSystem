namespace EMS.Application.DTOs.Files;

/// <summary>
/// Internal model the API uses to stream or delete a stored private file. Never returned from a controller:
/// <see cref="StoragePath"/> is a storage key relative to the private root and must not reach the browser.
/// </summary>
public sealed class StoredFileModel
{
    /// <summary>Storage key relative to <c>Storage:PrivateRoot</c> (legacy rows may still hold <c>/attachments/...</c>).</summary>
    public string StoragePath { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    /// <summary>Original file name offered to the browser in <c>Content-Disposition</c>.</summary>
    public string FileName { get; init; } = string.Empty;
}
