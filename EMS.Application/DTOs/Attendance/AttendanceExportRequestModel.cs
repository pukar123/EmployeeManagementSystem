namespace EMS.Application.DTOs.Attendance;

public class AttendanceExportRequestModel
{
    public int OrganizationId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public string Format { get; set; } = "csv";
}
