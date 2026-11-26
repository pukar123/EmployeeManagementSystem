using EMS.Application.DTOs.Files;

namespace EMS.Application.Services.Files;

/// <summary>
/// Thrown by <see cref="IUploadFileChecker"/> when a file is refused. Deliberately not a
/// <c>BusinessRuleException</c> or <see cref="InvalidOperationException"/>, so existing catch blocks cannot map it by
/// accident. <see cref="Exception.Message"/> is the user message; <see cref="FailedCheck"/> is for logs only and
/// never holds file contents.
/// </summary>
public sealed class UploadRejectedException : Exception
{
    public const string EmptyCode = "file_empty";
    public const string TooLargeCode = "file_too_large";
    public const string TypeNotAllowedCode = "file_type_not_allowed";
    public const string EmptyMessage = "The file is empty.";

    public UploadRejectedException(UploadPurpose purpose, UploadRejectionReason reason, string message, string failedCheck)
        : base(message)
    {
        Purpose = purpose;
        Reason = reason;
        FailedCheck = failedCheck;
    }

    public UploadPurpose Purpose { get; }

    public UploadRejectionReason Reason { get; }

    /// <summary>Stable error code for the API body: <c>file_empty</c>, <c>file_too_large</c> or <c>file_type_not_allowed</c>.</summary>
    public string Code => Reason switch
    {
        UploadRejectionReason.Empty => EmptyCode,
        UploadRejectionReason.TooLarge => TooLargeCode,
        _ => TypeNotAllowedCode,
    };

    /// <summary>Which check failed (for example <c>extension</c>, <c>declared content type</c>, <c>signature</c>). Logged, never shown.</summary>
    public string FailedCheck { get; }
}
