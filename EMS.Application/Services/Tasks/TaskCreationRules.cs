using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Pukar.Shared;

namespace EMS.Application.Services.Tasks;

internal static class TaskCreationRules
{
    public static TaskItem BuildOnboardingTask(
        Employee employee,
        OnboardingChecklistTemplateItem templateItem,
        int employeeOnboardingChecklistId,
        int? assignedByUserId,
        DateTime anchorDateUtc,
        DateTime nowUtc)
    {
        var title = StringHelper.NormalizeRequired(templateItem.Title);
        var dueAtUtc = templateItem.DefaultDueDaysFromStart.HasValue
            ? DateTime.SpecifyKind(anchorDateUtc.Date.AddDays(templateItem.DefaultDueDaysFromStart.Value), DateTimeKind.Utc)
            : (DateTime?)null;

        ValidateTimeframe(null, dueAtUtc);

        return new TaskItem
        {
            EmployeeId = employee.Id,
            OrganizationId = employee.OrganizationId,
            AssignedByUserId = assignedByUserId,
            Title = title,
            Description = templateItem.Description,
            Priority = templateItem.DefaultPriority,
            DueAtUtc = dueAtUtc,
            AssignedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Status = TaskWorkflowStatus.Assigned,
            OnboardingTemplateItemId = templateItem.Id,
            EmployeeOnboardingChecklistId = employeeOnboardingChecklistId,
        };
    }

    public static void ValidateTimeframe(DateTime? startAtUtc, DateTime? dueAtUtc)
    {
        ValidateUtcDate(startAtUtc, "Start date must include a valid UTC-aware timestamp.");
        ValidateUtcDate(dueAtUtc, "Due date must include a valid UTC-aware timestamp.");

        if (startAtUtc.HasValue && dueAtUtc.HasValue && startAtUtc.Value > dueAtUtc.Value)
            throw new BusinessRuleException("Start date must be before or equal to due date.");
    }

    private static void ValidateUtcDate(DateTime? value, string message)
    {
        if (value.HasValue && value.Value.Kind == DateTimeKind.Unspecified)
            throw new BusinessRuleException(message);
    }
}
