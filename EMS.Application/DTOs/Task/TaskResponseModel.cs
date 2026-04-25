using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Task;

public class TaskResponseModel
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int? OrganizationId { get; set; }
    public int? AssignedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskWorkflowStatus Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? StartAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
