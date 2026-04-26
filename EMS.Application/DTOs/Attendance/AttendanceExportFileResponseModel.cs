namespace EMS.Application.DTOs.Attendance;

public class AttendanceExportFileResponseModel
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileBytes { get; set; } = [];
}
