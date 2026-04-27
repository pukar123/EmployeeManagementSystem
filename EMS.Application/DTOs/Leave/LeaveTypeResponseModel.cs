using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Leave;

public class LeaveTypeResponseModel
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LeaveUnit Unit { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
