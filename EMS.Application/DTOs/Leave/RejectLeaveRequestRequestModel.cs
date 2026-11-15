namespace EMS.Application.DTOs.Leave;

public class RejectLeaveRequestRequestModel
{
    /// <summary>Required rejection reason shown to the employee (max 1000 characters).</summary>
    public string? Reason { get; set; }
}
