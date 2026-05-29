using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Shift;

public class UpdateShiftRequestModel
{
    public int? SiteId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public ShiftStatus Status { get; set; }
}
