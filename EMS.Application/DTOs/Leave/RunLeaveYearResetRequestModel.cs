namespace EMS.Application.DTOs.Leave;

public class RunLeaveYearResetRequestModel
{
    public int OrganizationId { get; set; }
    public DateTime LeaveYearStartDateUtc { get; set; }
}
