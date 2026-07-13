using System.Globalization;
using System.Text.Json;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pukar.Notifications.Application.DTOs;
using Pukar.Notifications.Application.Services;
using Pukar.Notifications.Domain.Enums;

namespace EMS.Application.Services.Notifications;

public interface IEmsNotificationProducer
{
    Task NotifyTaskAssignedAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task NotifyLeaveSubmittedAsync(LeaveRequest request, CancellationToken cancellationToken = default);

    Task NotifyLeaveApprovedAsync(LeaveRequest request, CancellationToken cancellationToken = default);

    Task NotifyLeaveRejectedAsync(LeaveRequest request, CancellationToken cancellationToken = default);

    Task NotifyInvitationDeliveryFailedAsync(
        int employeeId,
        int actorUserId,
        string? failureReason,
        CancellationToken cancellationToken = default);

    Task NotifyUpcomingScheduledChangeAsync(
        EmployeeScheduledChange change,
        CancellationToken cancellationToken = default);

    Task NotifyDocumentExpiringAsync(
        Document document,
        int employeeId,
        int daysUntilExpiry,
        CancellationToken cancellationToken = default);
}

public sealed class EmsNotificationProducer : IEmsNotificationProducer
{
    private readonly INotificationService _notifications;
    private readonly EmployeeNotificationRecipientResolver _recipientResolver;
    private readonly IBaseRepository<Employee> _employees;
    private readonly ILogger<EmsNotificationProducer> _logger;

    public EmsNotificationProducer(
        INotificationService notifications,
        EmployeeNotificationRecipientResolver recipientResolver,
        IBaseRepository<Employee> employees,
        ILogger<EmsNotificationProducer> logger)
    {
        _notifications = notifications;
        _recipientResolver = recipientResolver;
        _employees = employees;
        _logger = logger;
    }

    public Task NotifyTaskAssignedAsync(TaskItem task, CancellationToken cancellationToken = default)
        => SafeNotifyAsync(async () =>
        {
            var recipientUserId = await _recipientResolver.TryResolveUserIdByEmployeeIdAsync(
                task.EmployeeId,
                cancellationToken);
            if (recipientUserId is null)
                return;

            await _notifications.CreateAsync(
                new CreateNotificationRequestModel
                {
                    RecipientUserId = recipientUserId,
                    TypeKey = EmsNotificationTypeKeys.TaskAssigned,
                    Title = "New task assigned",
                    Body = $"You have been assigned a new task: {task.Title}.",
                    ActionUrl = "/tasks",
                    Severity = NotificationSeverity.Normal,
                    DedupeKey = $"task-assigned:{task.Id}",
                    MetadataJson = JsonSerializer.Serialize(new { taskId = task.Id, employeeId = task.EmployeeId }),
                },
                cancellationToken);
        }, cancellationToken);

    public Task NotifyLeaveSubmittedAsync(LeaveRequest request, CancellationToken cancellationToken = default)
        => SafeNotifyAsync(async () =>
        {
            var employee = await _employees.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsArchived, cancellationToken);
            if (employee?.ManagerId is not int managerId)
                return;

            var managerUserId = await _recipientResolver.TryResolveUserIdByEmployeeIdAsync(managerId, cancellationToken);
            if (managerUserId is null)
                return;

            var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
            await _notifications.CreateAsync(
                new CreateNotificationRequestModel
                {
                    RecipientUserId = managerUserId,
                    TypeKey = EmsNotificationTypeKeys.LeaveSubmitted,
                    Title = "Leave request pending approval",
                    Body = $"{employeeName} submitted a leave request awaiting your review.",
                    ActionUrl = "/leave",
                    Severity = NotificationSeverity.High,
                    DedupeKey = $"leave-submitted:{request.Id}",
                    MetadataJson = JsonSerializer.Serialize(new
                    {
                        leaveRequestId = request.Id,
                        employeeId = request.EmployeeId,
                    }),
                },
                cancellationToken);
        }, cancellationToken);

    public Task NotifyLeaveApprovedAsync(LeaveRequest request, CancellationToken cancellationToken = default)
        => NotifyLeaveDecisionAsync(
            request,
            EmsNotificationTypeKeys.LeaveApproved,
            "Leave request approved",
            "Your leave request has been approved.",
            cancellationToken);

    public Task NotifyLeaveRejectedAsync(LeaveRequest request, CancellationToken cancellationToken = default)
        => NotifyLeaveDecisionAsync(
            request,
            EmsNotificationTypeKeys.LeaveRejected,
            "Leave request rejected",
            "Your leave request has been rejected.",
            NotificationSeverity.High,
            cancellationToken);

    public Task NotifyInvitationDeliveryFailedAsync(
        int employeeId,
        int actorUserId,
        string? failureReason,
        CancellationToken cancellationToken = default)
        => SafeNotifyAsync(async () =>
        {
            var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null)
                return;

            var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
            var reason = string.IsNullOrWhiteSpace(failureReason)
                ? "The invitation email could not be delivered."
                : failureReason.Trim();

            await _notifications.CreateAsync(
                new CreateNotificationRequestModel
                {
                    RecipientUserId = actorUserId,
                    TypeKey = EmsNotificationTypeKeys.InvitationFailed,
                    Title = "Employee invitation delivery failed",
                    Body = $"Invitation for {employeeName} failed: {reason}",
                    ActionUrl = $"/employees/{employeeId}",
                    Severity = NotificationSeverity.High,
                    DedupeKey = $"invitation-failed:{employeeId}:{DateTime.UtcNow:yyyyMMdd}",
                    MetadataJson = JsonSerializer.Serialize(new { employeeId }),
                },
                cancellationToken);
        }, cancellationToken);

    public Task NotifyUpcomingScheduledChangeAsync(
        EmployeeScheduledChange change,
        CancellationToken cancellationToken = default)
        => SafeNotifyAsync(async () =>
        {
            var employee = await _employees.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == change.EmployeeId && !e.IsArchived, cancellationToken);
            if (employee is null)
                return;

            var effectiveDate = change.EffectiveAtUtc.ToString("d", CultureInfo.InvariantCulture);
            var changeLabel = FormatScheduledChangeType(change.ChangeType);
            var body = $"A {changeLabel} is scheduled for {effectiveDate}.";

            var requests = new List<CreateNotificationRequestModel>();
            var employeeUserId = EmployeeNotificationRecipientResolver.TryResolveUserId(employee);
            if (employeeUserId is int userId)
            {
                requests.Add(BuildScheduledChangeNotification(
                    userId,
                    change,
                    body,
                    "/employee-portal"));
            }

            if (employee.ManagerId is int managerId)
            {
                var managerUserId = await _recipientResolver.TryResolveUserIdByEmployeeIdAsync(managerId, cancellationToken);
                if (managerUserId is int managerUser)
                {
                    var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
                    requests.Add(BuildScheduledChangeNotification(
                        managerUser,
                        change,
                        $"{employeeName}: {body}",
                        "/employees"));
                }
            }

            if (requests.Count == 0)
                return;

            await _notifications.CreateBulkAsync(requests, cancellationToken);
        }, cancellationToken);

    public Task NotifyDocumentExpiringAsync(
        Document document,
        int employeeId,
        int daysUntilExpiry,
        CancellationToken cancellationToken = default)
        => SafeNotifyAsync(async () =>
        {
            var recipientUserId = await _recipientResolver.TryResolveUserIdByEmployeeIdAsync(employeeId, cancellationToken);
            if (recipientUserId is null)
                return;

            var expiryText = document.ExpiryDate?.ToString("d", CultureInfo.InvariantCulture) ?? "soon";
            await _notifications.CreateAsync(
                new CreateNotificationRequestModel
                {
                    RecipientUserId = recipientUserId,
                    TypeKey = EmsNotificationTypeKeys.DocumentExpiring,
                    Title = "Document expiring soon",
                    Body = $"Document \"{document.Name}\" expires on {expiryText} ({daysUntilExpiry} day(s) remaining).",
                    ActionUrl = "/documents",
                    Severity = daysUntilExpiry <= 7 ? NotificationSeverity.High : NotificationSeverity.Normal,
                    DedupeKey = $"document-expiring:{document.Id}:{daysUntilExpiry}",
                    MetadataJson = JsonSerializer.Serialize(new { documentId = document.Id, employeeId }),
                },
                cancellationToken);
        }, cancellationToken);

    private Task NotifyLeaveDecisionAsync(
        LeaveRequest request,
        string typeKey,
        string title,
        string body,
        CancellationToken cancellationToken)
        => NotifyLeaveDecisionAsync(request, typeKey, title, body, NotificationSeverity.Normal, cancellationToken);

    private Task NotifyLeaveDecisionAsync(
        LeaveRequest request,
        string typeKey,
        string title,
        string body,
        NotificationSeverity severity,
        CancellationToken cancellationToken)
        => SafeNotifyAsync(async () =>
        {
            var recipientUserId = await _recipientResolver.TryResolveUserIdByEmployeeIdAsync(
                request.EmployeeId,
                cancellationToken);
            if (recipientUserId is null)
                return;

            await _notifications.CreateAsync(
                new CreateNotificationRequestModel
                {
                    RecipientUserId = recipientUserId,
                    TypeKey = typeKey,
                    Title = title,
                    Body = body,
                    ActionUrl = "/leave",
                    Severity = severity,
                    DedupeKey = $"{typeKey}:{request.Id}",
                    MetadataJson = JsonSerializer.Serialize(new
                    {
                        leaveRequestId = request.Id,
                        employeeId = request.EmployeeId,
                    }),
                },
                cancellationToken);
        }, cancellationToken);

    private static CreateNotificationRequestModel BuildScheduledChangeNotification(
        int recipientUserId,
        EmployeeScheduledChange change,
        string body,
        string actionUrl)
        => new()
        {
            RecipientUserId = recipientUserId,
            TypeKey = EmsNotificationTypeKeys.ScheduledChangeUpcoming,
            Title = "Upcoming employee change",
            Body = body,
            ActionUrl = actionUrl,
            Severity = NotificationSeverity.Normal,
            DedupeKey = $"scheduled-change-upcoming:{change.Id}:{recipientUserId}:{DateTime.UtcNow:yyyyMMdd}",
            MetadataJson = JsonSerializer.Serialize(new
            {
                scheduledChangeId = change.Id,
                employeeId = change.EmployeeId,
                changeType = change.ChangeType.ToString(),
            }),
        };

    private static string FormatScheduledChangeType(EmployeeScheduledChangeType changeType)
        => changeType switch
        {
            EmployeeScheduledChangeType.ChangeEmploymentStatus => "status change",
            EmployeeScheduledChangeType.TransferDepartment => "department change",
            EmployeeScheduledChangeType.TransferPosition => "position change",
            EmployeeScheduledChangeType.TransferManager => "manager change",
            EmployeeScheduledChangeType.Terminate => "termination",
            EmployeeScheduledChangeType.Archive => "archive",
            EmployeeScheduledChangeType.RestoreRecord => "restore",
            _ => "scheduled change",
        };

    private async Task SafeNotifyAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "EMS notification producer failed.");
        }
    }
}
