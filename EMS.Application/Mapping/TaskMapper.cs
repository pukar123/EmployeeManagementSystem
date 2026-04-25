using TaskDtos = EMS.Application.DTOs.Task;
using TaskEntity = EMS.Domain.DbModels.TaskItem;

namespace EMS.Application.Mapping;

internal static class TaskMapper
{
    public static TaskEntity ToEntity(TaskDtos.CreateTaskRequestModel request)
    {
        return new TaskEntity
        {
            EmployeeId = request.EmployeeId,
            OrganizationId = request.OrganizationId,
            Title = request.Title,
            Description = request.Description,
            StartAtUtc = request.StartAtUtc,
            DueAtUtc = request.DueAtUtc,
            Priority = request.Priority,
        };
    }

    public static void ApplyUpdate(TaskEntity entity, TaskDtos.UpdateTaskRequestModel request)
    {
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.StartAtUtc = request.StartAtUtc;
        entity.DueAtUtc = request.DueAtUtc;
        entity.Priority = request.Priority;
    }

    public static TaskDtos.TaskResponseModel ToResponse(TaskEntity entity)
    {
        return new TaskDtos.TaskResponseModel
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            OrganizationId = entity.OrganizationId,
            AssignedByUserId = entity.AssignedByUserId,
            Title = entity.Title,
            Description = entity.Description,
            Status = entity.Status,
            Priority = entity.Priority,
            AssignedAtUtc = entity.AssignedAtUtc,
            StartAtUtc = entity.StartAtUtc,
            DueAtUtc = entity.DueAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }
}
