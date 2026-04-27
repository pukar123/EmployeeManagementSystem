namespace EMS.Application.DTOs.Leave;

public class LeaveRequestAttachmentResponseModel
{
    public int Id { get; set; }
    public int LeaveRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
