using EMS.Application.DTOs.Files;

namespace EMS.Application.Services.Files;

/// <summary>The allowed types, maximum size and user messages for one <see cref="UploadPurpose"/>.</summary>
public sealed class UploadRule
{
    public UploadRule(
        UploadPurpose purpose,
        long maxBytes,
        IReadOnlyList<AllowedUploadType> allowedTypes,
        string allowedTypesLabel,
        string fallbackBaseName)
    {
        Purpose = purpose;
        MaxBytes = maxBytes;
        AllowedTypes = allowedTypes;
        AllowedTypesLabel = allowedTypesLabel;
        FallbackBaseName = fallbackBaseName;
    }

    public UploadPurpose Purpose { get; }

    /// <summary>Largest accepted file in bytes. A file of exactly this size is accepted.</summary>
    public long MaxBytes { get; }

    public IReadOnlyList<AllowedUploadType> AllowedTypes { get; }

    /// <summary>Allowed types as shown to the user (for example <c>PDF, PNG, JPEG</c>).</summary>
    public string AllowedTypesLabel { get; }

    /// <summary>Base name used when the uploaded name has nothing left after sanitising.</summary>
    public string FallbackBaseName { get; }

    /// <summary>Largest accepted request body: the file limit plus 1 MB for form fields and multipart overhead.</summary>
    public long MaxRequestBodyBytes => MaxBytes + UploadRules.OneMegabyte;

    public string TypeNotAllowedMessage => $"This file type is not allowed. Allowed types: {AllowedTypesLabel}.";

    public string TooLargeMessage => $"The file is too large. The maximum size is {MaxBytes / UploadRules.OneMegabyte} MB.";
}
