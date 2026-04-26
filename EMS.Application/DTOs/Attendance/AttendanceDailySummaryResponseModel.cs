namespace EMS.Application.DTOs.Attendance;

public class AttendanceDailySummaryResponseModel
{
    public DateTime WorkDate { get; set; }
    public int OrganizationId { get; set; }
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public int TotalRecords { get; set; }
    public int PresentEmployees { get; set; }
    public int TotalWorkedMinutes { get; set; }
    public int TotalBreakMinutes { get; set; }
    public decimal AverageWorkedMinutes { get; set; }
}
