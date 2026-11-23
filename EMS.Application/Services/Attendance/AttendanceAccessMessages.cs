namespace EMS.Application.Services.Attendance;

/// <summary>
/// Messages for attendance access decisions. The controller turns <see cref="Denied"/> into 403 with no body and
/// <see cref="ChooseEmployee"/> into 400.
/// </summary>
public static class AttendanceAccessMessages
{
    public const string Denied = "Attendance access denied.";

    /// <summary>An attendance admin with no linked employee sent a self action without saying whose it is.</summary>
    public const string ChooseEmployee = "Choose an employee.";
}
