using EMS.Application.DTOs.Task;
using EMS.Application.Mapping;
using EMS.Application.Services.Notifications;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Tasks;

public sealed class TaskService : ITaskService
{
    private readonly IBaseRepository<TaskItem> _taskRepository;
    private readonly IBaseRepository<Employee> _employeeRepository;
    private readonly IEmsNotificationProducer _notificationProducer;

    public TaskService(
        IBaseRepository<TaskItem> taskRepository,
        IBaseRepository<Employee> employeeRepository,
        IEmsNotificationProducer notificationProducer)
    {
        _taskRepository = taskRepository;
        _employeeRepository = employeeRepository;
        _notificationProducer = notificationProducer;
    }

    public async Task<TaskResponseModel> CreateAsync(
        CreateTaskRequestModel request,
        int? assignedByUserId,
        CancellationToken cancellationToken = default)
    {
        request.Title = StringHelper.NormalizeRequired(request.Title);
        TaskCreationRules.ValidateTimeframe(request.StartAtUtc, request.DueAtUtc);

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
            throw new BusinessRuleException("Employee was not found.");

        if (request.OrganizationId.HasValue && employee.OrganizationId != request.OrganizationId.Value)
            throw new BusinessRuleException("Task organization must match the employee organization.");

        var now = DateTime.UtcNow;
        var entity = TaskMapper.ToEntity(request);
        entity.OrganizationId ??= employee.OrganizationId;
        entity.AssignedByUserId = assignedByUserId;
        entity.AssignedAtUtc = now;
        entity.CreatedAtUtc = now;
        entity.UpdatedAtUtc = now;
        entity.Status = TaskWorkflowStatus.Assigned;

        await _taskRepository.AddAsync(entity, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        await _notificationProducer.NotifyTaskAssignedAsync(entity, cancellationToken);

        return TaskMapper.ToResponse(entity);
    }

    public async Task<TaskResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _taskRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : TaskMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<TaskResponseModel>> GetAllAsync(
        int? employeeId = null,
        int? assignedByUserId = null,
        DateTime? rangeStartUtc = null,
        DateTime? rangeEndUtc = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(rangeStartUtc, rangeEndUtc);

        var query = _taskRepository.GetQueryable().AsNoTracking();

        if (employeeId.HasValue)
            query = query.Where(t => t.EmployeeId == employeeId.Value);

        if (assignedByUserId.HasValue)
            query = query.Where(t => t.AssignedByUserId == assignedByUserId.Value);

        if (rangeStartUtc.HasValue || rangeEndUtc.HasValue)
        {
            var effectiveStart = rangeStartUtc ?? DateTime.MinValue;
            var effectiveEnd = rangeEndUtc ?? DateTime.MaxValue;

            query = query.Where(t =>
                (t.StartAtUtc.HasValue ? t.StartAtUtc.Value : (t.DueAtUtc ?? t.AssignedAtUtc)) <= effectiveEnd
                && (t.DueAtUtc ?? t.StartAtUtc ?? t.AssignedAtUtc) >= effectiveStart);
        }

        var list = await query
            .OrderByDescending(t => t.UpdatedAtUtc)
            .ThenBy(t => t.Title)
            .ToListAsync(cancellationToken);

        return list.Select(TaskMapper.ToResponse).ToList();
    }

    public async Task<TaskResponseModel?> UpdateAsync(int id, UpdateTaskRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        request.Title = StringHelper.NormalizeRequired(request.Title);
        TaskCreationRules.ValidateTimeframe(request.StartAtUtc, request.DueAtUtc);

        TaskMapper.ApplyUpdate(entity, request);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _taskRepository.Update(entity);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return TaskMapper.ToResponse(entity);
    }

    public async Task<TaskResponseModel?> UpdateStatusAsync(int id, UpdateTaskStatusRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        EnsureValidTransition(entity.Status, request.Status);
        entity.Status = request.Status;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _taskRepository.Update(entity);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return TaskMapper.ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        _taskRepository.Remove(entity);
        await _taskRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateDateRange(DateTime? rangeStartUtc, DateTime? rangeEndUtc)
    {
        TaskCreationRules.ValidateTimeframe(rangeStartUtc, rangeEndUtc);
    }

    private static void EnsureValidTransition(TaskWorkflowStatus current, TaskWorkflowStatus next)
    {
        if (current == next)
            return;

        var allowed = current switch
        {
            TaskWorkflowStatus.Assigned => next is TaskWorkflowStatus.InProgress or TaskWorkflowStatus.Blocked or TaskWorkflowStatus.Completed,
            TaskWorkflowStatus.InProgress => next is TaskWorkflowStatus.Blocked or TaskWorkflowStatus.Completed,
            TaskWorkflowStatus.Blocked => next is TaskWorkflowStatus.InProgress or TaskWorkflowStatus.Completed,
            TaskWorkflowStatus.Completed => false,
            _ => false,
        };

        if (!allowed)
            throw new BusinessRuleException($"Cannot change task status from {current} to {next}.");
    }
}
