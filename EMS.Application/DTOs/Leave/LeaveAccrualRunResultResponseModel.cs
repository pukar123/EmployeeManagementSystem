namespace EMS.Application.DTOs.Leave;

public class LeaveAccrualRunResultResponseModel
{
    public int OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public int EmployeesProcessed { get; set; }
    public int BalancesCreated { get; set; }
    public int BalancesUpdated { get; set; }
}
