namespace EMS.Application.Services.Notifications;

public static class EmsNotificationTypeKeys
{
    public const string TaskAssigned = "task.assigned";
    public const string LeaveSubmitted = "leave.submitted";
    public const string LeaveApproved = "leave.approved";
    public const string LeaveRejected = "leave.rejected";
    public const string ScheduledChangeUpcoming = "employee.scheduled_change.upcoming";
    public const string InvitationFailed = "employee.invitation.failed";
    public const string DocumentExpiring = "document.expiring";
}
