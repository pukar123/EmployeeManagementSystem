namespace EMS.Domain.DbModels;

public class LeaveRequestAttachment
{
    public int Id { get; set; }
    public int LeaveRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public LeaveRequest LeaveRequest { get; set; } = null!;
}
