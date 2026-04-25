using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Task;

public class UpdateTaskRequestModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public TaskPriority? Priority { get; set; }
}
