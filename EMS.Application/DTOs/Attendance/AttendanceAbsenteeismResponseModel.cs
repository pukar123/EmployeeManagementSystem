namespace EMS.Application.DTOs.Attendance;

public class AttendanceAbsenteeismResponseModel
{
    public int OrganizationId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public int ExpectedWorkDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public decimal AbsenceRate { get; set; }
}
