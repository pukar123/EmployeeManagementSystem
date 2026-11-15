namespace EMS.Application.DTOs.Leave;

public class ApproveLeaveRequestRequestModel
{
    /// <summary>Optional reviewer comment (max 1000 characters).</summary>
    public string? Comment { get; set; }
}
