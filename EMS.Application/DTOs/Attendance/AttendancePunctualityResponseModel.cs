namespace EMS.Application.DTOs.Attendance;

public class AttendancePunctualityResponseModel
{
    public int OrganizationId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public int TotalRecords { get; set; }
    public int LateArrivals { get; set; }
    public int EarlyDepartures { get; set; }
    public decimal LateArrivalRate { get; set; }
    public decimal EarlyDepartureRate { get; set; }
}
