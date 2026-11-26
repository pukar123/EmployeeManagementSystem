namespace EMS.Application.DTOs.Files;

/// <summary>Why the upload checker refused a file.</summary>
public enum UploadRejectionReason
{
    /// <summary>The file has no content (0 bytes).</summary>
    Empty = 1,

    /// <summary>The file is over the purpose's maximum size.</summary>
    TooLarge = 2,

    /// <summary>The extension, declared content type and content signature do not all match one allowed type.</summary>
    TypeNotAllowed = 3,
}
