namespace EMS.Application.DTOs.Leave;

public class LeaveAdminSummaryResponseModel
{
    public int OrganizationId { get; set; }
    public DateTime AsOfDateUtc { get; set; }
    public int AppliedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int CancelledCount { get; set; }
    public int CurrentlyOnLeaveCount { get; set; }
}
