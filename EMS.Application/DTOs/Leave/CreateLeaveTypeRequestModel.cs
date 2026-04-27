using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Leave;

public class CreateLeaveTypeRequestModel
{
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LeaveUnit Unit { get; set; } = LeaveUnit.Days;
    public bool RequiresAttachment { get; set; }
    public bool IsActive { get; set; } = true;
}
