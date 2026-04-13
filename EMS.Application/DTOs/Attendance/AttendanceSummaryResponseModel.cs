namespace EMS.Application.DTOs.Attendance;

public class AttendanceSummaryResponseModel
{
    public int EmployeeId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalRecords { get; set; }
    public int TotalBreakMinutes { get; set; }
    public int TotalWorkedMinutes { get; set; }
}
