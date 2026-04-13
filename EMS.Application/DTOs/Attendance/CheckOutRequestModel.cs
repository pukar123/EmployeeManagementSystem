namespace EMS.Application.DTOs.Attendance;

public class CheckOutRequestModel
{
    public int EmployeeId { get; set; }
    public DateTime? CheckOutAtUtc { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
