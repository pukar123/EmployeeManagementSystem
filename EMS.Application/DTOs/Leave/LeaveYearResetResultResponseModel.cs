namespace EMS.Application.DTOs.Leave;

public class LeaveYearResetResultResponseModel
{
    public int OrganizationId { get; set; }
    public DateTime LeaveYearStartDateUtc { get; set; }
    public int BalancesReset { get; set; }
}
