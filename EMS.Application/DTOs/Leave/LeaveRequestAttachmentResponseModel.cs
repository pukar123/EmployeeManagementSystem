namespace EMS.Application.DTOs.Leave;

public class LeaveRequestAttachmentResponseModel
{
    public int Id { get; set; }
    public int LeaveRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Authorised download endpoint relative to the API origin (<c>/api/LeaveAttachments/{id}/file</c>). The storage
    /// path is never returned: clients download through this endpoint with a signed-in request.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
