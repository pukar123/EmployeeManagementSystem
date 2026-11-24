namespace EMS.Domain.DbModels;

public class LeaveRequestAttachment
{
    public int Id { get; set; }
    public int LeaveRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Storage key relative to <c>Storage:PrivateRoot</c> (e.g. <c>Leave/12/{guid}.pdf</c>). Never public and never
    /// returned to clients. Rows saved before B08 may hold <c>/attachments/Leave/...</c> until the move step runs.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public LeaveRequest LeaveRequest { get; set; } = null!;
}
