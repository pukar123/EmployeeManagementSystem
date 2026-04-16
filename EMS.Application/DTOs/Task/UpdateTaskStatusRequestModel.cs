using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Task;

public class UpdateTaskStatusRequestModel
{
    public TaskWorkflowStatus Status { get; set; }
}
