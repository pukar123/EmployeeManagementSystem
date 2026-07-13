namespace EMS.Application.DTOs.Manager;

public sealed class ManagerTeamSummaryResponseModel
{
    public int ActiveEmployeeCount { get; set; }
    public int PendingLeaveRequestCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int UpcomingScheduledChangeCount { get; set; }
    public ManagerTeamAttendanceTodaySummaryModel AttendanceToday { get; set; } = new();
}

public sealed class ManagerTeamAttendanceTodaySummaryModel
{
    public DateTime WorkDateUtc { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int OnLeaveCount { get; set; }
    public int CheckedInOpenCount { get; set; }
}
