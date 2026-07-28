using EMS.Application.DTOs.Onboarding;
using EMS.Application.Mapping;
using EMS.Application.Services.Tasks;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Onboarding;

public sealed class OnboardingChecklistService : IOnboardingChecklistService
{
    private readonly IBaseRepository<OnboardingChecklistTemplate> _templateRepository;
    private readonly IBaseRepository<EmployeeOnboardingChecklist> _checklistRepository;
    private readonly IBaseRepository<TaskItem> _taskRepository;

    public OnboardingChecklistService(
        IBaseRepository<OnboardingChecklistTemplate> templateRepository,
        IBaseRepository<EmployeeOnboardingChecklist> checklistRepository,
        IBaseRepository<TaskItem> taskRepository)
    {
        _templateRepository = templateRepository;
        _checklistRepository = checklistRepository;
        _taskRepository = taskRepository;
    }

    public async Task GenerateForEmployeeAsync(
        Employee employee,
        int templateId,
        int? assignedByUserId,
        DateTime anchorDateUtc,
        CancellationToken cancellationToken = default)
    {
        if (employee.EmploymentStatus != EmploymentStatus.Preboarding)
            throw new BusinessRuleException("Onboarding tasks can only be generated for preboarding employees.");

        var existing = await _checklistRepository.GetQueryable()
            .AnyAsync(c => c.EmployeeId == employee.Id && employee.Id > 0, cancellationToken);
        if (existing)
            throw new BusinessRuleException("This employee already has an onboarding checklist.");

        var template = await _templateRepository.GetQueryable()
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken)
            ?? throw new BusinessRuleException("Onboarding checklist template was not found.");

        if (!template.IsActive)
            throw new BusinessRuleException("Onboarding checklist template is not active.");

        if (template.OrganizationId != employee.OrganizationId)
            throw new BusinessRuleException("Onboarding checklist template must belong to the employee organization.");

        if (template.Items.Count == 0)
            throw new BusinessRuleException("Onboarding checklist template has no items.");

        var now = DateTime.UtcNow;
        var checklist = new EmployeeOnboardingChecklist
        {
            Employee = employee,
            TemplateId = template.Id,
            GeneratedAtUtc = now,
            GeneratedByUserId = assignedByUserId,
        };

        await _checklistRepository.AddAsync(checklist, cancellationToken);

        var orderedItems = template.Items.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToList();
        var tasks = orderedItems
            .Select(item => BuildTask(employee, item, checklist, assignedByUserId, anchorDateUtc, now))
            .ToList();

        await _taskRepository.AddRangeAsync(tasks, cancellationToken);
    }

    public async Task<EmployeeOnboardingProgressResponseModel> GetProgressAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var checklist = await _checklistRepository.GetQueryable()
            .AsNoTracking()
            .Include(c => c.Template)
                .ThenInclude(t => t.Items)
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.EmployeeId == employeeId, cancellationToken);

        if (checklist is null)
            return new EmployeeOnboardingProgressResponseModel();

        var now = DateTime.UtcNow;
        var tasksByTemplateItemId = checklist.Tasks
            .Where(t => t.OnboardingTemplateItemId.HasValue)
            .GroupBy(t => t.OnboardingTemplateItemId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.UpdatedAtUtc).First());

        var items = checklist.Template.Items
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(templateItem =>
            {
                tasksByTemplateItemId.TryGetValue(templateItem.Id, out var task);
                var isOverdue = task is not null
                    && task.Status != TaskWorkflowStatus.Completed
                    && task.DueAtUtc.HasValue
                    && task.DueAtUtc.Value < now;

                return new EmployeeOnboardingProgressItemResponseModel
                {
                    TemplateItemId = templateItem.Id,
                    Category = templateItem.Category,
                    Title = templateItem.Title,
                    IsRequired = templateItem.IsRequired,
                    SortOrder = templateItem.SortOrder,
                    TaskId = task?.Id,
                    Status = task?.Status,
                    DueAtUtc = task?.DueAtUtc,
                    IsOverdue = isOverdue,
                };
            })
            .ToList();

        var totalCount = items.Count;
        var completedCount = items.Count(i => i.Status == TaskWorkflowStatus.Completed);
        var overdueCount = items.Count(i => i.IsOverdue);
        var percentComplete = totalCount == 0
            ? 0
            : (int)Math.Round(completedCount * 100.0 / totalCount, MidpointRounding.AwayFromZero);

        return new EmployeeOnboardingProgressResponseModel
        {
            TemplateId = checklist.TemplateId,
            TemplateName = checklist.Template.Name,
            GeneratedAtUtc = checklist.GeneratedAtUtc,
            TotalCount = totalCount,
            CompletedCount = completedCount,
            OverdueCount = overdueCount,
            PercentComplete = percentComplete,
            Items = items,
        };
    }

    private static TaskItem BuildTask(
        Employee employee,
        OnboardingChecklistTemplateItem templateItem,
        EmployeeOnboardingChecklist checklist,
        int? assignedByUserId,
        DateTime anchorDateUtc,
        DateTime nowUtc)
    {
        var task = TaskCreationRules.BuildOnboardingTask(
            employee,
            templateItem,
            employeeOnboardingChecklistId: 0,
            assignedByUserId,
            anchorDateUtc,
            nowUtc);

        task.Employee = employee;
        task.EmployeeOnboardingChecklist = checklist;
        task.OnboardingTemplateItemId = templateItem.Id;
        return task;
    }
}
