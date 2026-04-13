namespace EMS.Application.DTOs.Attendance;

public class CheckInRequestModel
{
    public int EmployeeId { get; set; }
    public DateTime? CheckInAtUtc { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
