namespace EMS.Application.DTOs.Manager;

public sealed class ManagerLeaveApprovalQueryModel
{
    public int OrganizationId { get; set; }

    /// <summary>Admins only: view another manager's queue (same rule as the team dashboard).</summary>
    public int? ManagerId { get; set; }
}
