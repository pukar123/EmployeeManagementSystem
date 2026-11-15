namespace EMS.Application.Services.Leave;

/// <summary>
/// Stable messages for the leave approval workflow. The API maps these to HTTP status codes,
/// so change them only together with <c>LeaveRequestsController</c>.
/// </summary>
public static class LeaveApprovalMessages
{
    // 403
    public const string Denied = "You are not allowed to approve or reject this leave request.";
    public const string ReviewerNotLinked =
        "Your user account is not linked to an employee record, so you cannot approve or reject leave requests.";
    public const string SelfDecisionDenied = "You cannot approve or reject your own leave request.";

    // 409
    public const string ConcurrencyConflict =
        "This leave request was changed by someone else. Refresh and try again.";

    // 400
    public const string NotDecidable = "Only pending leave requests can be approved or rejected.";
    public const string InsufficientBalance = "Insufficient leave balance to approve this request.";
    public const string BalanceMissing = "No leave balance exists for this employee and leave type.";
    public const string RejectionReasonRequired = "A reason is required to reject a leave request.";
    public const string CommentTooLong = "Decision comment cannot exceed 1000 characters.";

    public const int MaxCommentLength = 1000;

    public static bool IsAccessDenied(string message)
        => string.Equals(message, Denied, StringComparison.Ordinal)
           || string.Equals(message, ReviewerNotLinked, StringComparison.Ordinal)
           || string.Equals(message, SelfDecisionDenied, StringComparison.Ordinal);
}
