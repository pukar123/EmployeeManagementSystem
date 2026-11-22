namespace EMS.Application.Services.Leave;

/// <summary>Messages for leave access denials. Controllers turn <see cref="Denied"/> into 403 with no body.</summary>
public static class LeaveAccessMessages
{
    public const string Denied = "Leave access denied.";
}
