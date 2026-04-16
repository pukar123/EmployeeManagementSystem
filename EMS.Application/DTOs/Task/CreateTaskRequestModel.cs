using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Task;

public class CreateTaskRequestModel
{
    public int EmployeeId { get; set; }
    public int? OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public TaskPriority? Priority { get; set; }
}
