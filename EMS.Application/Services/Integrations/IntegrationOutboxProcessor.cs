using System.Text.Json;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Integrations;

public sealed class IntegrationOutboxProcessor : IIntegrationOutboxProcessor
{
    // Maximum attempts before a transient failure is dead-lettered as Failed.
    private const int MaxAttempts = 10;

    // Rows stuck in Processing longer than this are considered orphaned by a crashed
    // dispatcher and are recovered on the next pass.
    private static readonly TimeSpan StaleProcessingThreshold = TimeSpan.FromMinutes(5);

    private readonly IBaseRepository<IntegrationOutboxMessage> _outbox;
    private readonly IBaseRepository<Employee> _employees;
    private readonly IEmployeeUserManagementGateway _gateway;
    private readonly IEmployeeRoleSyncService _roleSyncService;
    private readonly IEmployeeInvitationService _invitationService;

    public IntegrationOutboxProcessor(
        IBaseRepository<IntegrationOutboxMessage> outbox,
        IBaseRepository<Employee> employees,
        IEmployeeUserManagementGateway gateway,
        IEmployeeRoleSyncService roleSyncService,
        IEmployeeInvitationService invitationService)
    {
        _outbox = outbox;
        _employees = employees;
        _gateway = gateway;
        _roleSyncService = roleSyncService;
        _invitationService = invitationService;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleBefore = now - StaleProcessingThreshold;

        var pending = await _outbox.GetQueryable()
            .Where(m =>
                (m.Status == IntegrationOutboxStatus.Pending && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now))
                // Recover orphaned rows left in Processing by a crashed/restarted dispatcher.
                || (m.Status == IntegrationOutboxStatus.Processing && m.ProcessedAtUtc == null && m.CreatedAtUtc <= staleBefore))
            .OrderBy(m => m.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var message in pending)
        {
            message.Status = IntegrationOutboxStatus.Processing;
            message.AttemptCount++;
            _outbox.Update(message);
            await _outbox.SaveChangesAsync(cancellationToken);

            try
            {
                await DispatchAsync(message, cancellationToken);
                message.Status = IntegrationOutboxStatus.Succeeded;
                message.ProcessedAtUtc = DateTime.UtcNow;
                message.LastError = null;
                processed++;
            }
            catch (ConflictBusinessRuleException ex)
            {
                // Permanent business conflict (e.g. email already belongs to an active or
                // administrator account, or is linked to another employee). Retrying will
                // never succeed, so dead-letter as Failed with a clear reason.
                message.Status = IntegrationOutboxStatus.Failed;
                message.LastError = Truncate(ex.Message);
            }
            catch (Exception ex)
            {
                // Keep pending and retry safely — never report identity changes as successful.
                message.LastError = Truncate(ex.Message);
                if (message.AttemptCount >= MaxAttempts)
                {
                    message.Status = IntegrationOutboxStatus.Failed;
                }
                else
                {
                    message.Status = IntegrationOutboxStatus.Pending;
                    message.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(Math.Min(message.AttemptCount * 5, 60));
                }
            }

            _outbox.Update(message);
            await _outbox.SaveChangesAsync(cancellationToken);
        }

        return processed;
    }

    private async Task DispatchAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(message.PayloadJson);
        var employeeId = doc.RootElement.GetProperty("employeeId").GetInt32();
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException($"Employee {employeeId} not found for outbox message.");

        switch (message.MessageType)
        {
            case IntegrationOutboxMessageType.RevokeEmployeeLinkedIdentity:
                await EmployeeLinkedIdentityHelper.RevokeLinkedIdentityAsync(
                    employee,
                    _gateway,
                    message.IdempotencyKey,
                    cancellationToken);
                break;
            case IntegrationOutboxMessageType.SyncEmployeeRoles:
                await _roleSyncService.SyncEmployeeAsync(employeeId, cancellationToken);
                break;
            case IntegrationOutboxMessageType.ProvisionEmployeeIdentity:
                // Idempotent: creates an inactive UM account + invitation when the email is
                // new (or links an eligible existing invitation), links Employee.ExternalIdentityKey,
                // then synchronizes effective roles. Safe to retry after a crash because
                // invitation creation is keyed by the employee correlation id and the link
                // update is a no-op when already set.
                await _invitationService.SendAsync(employeeId, cancellationToken);
                await _roleSyncService.SyncEmployeeAsync(employeeId, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unknown outbox message type {message.MessageType}.");
        }
    }

    private static string Truncate(string value)
        => value.Length > 2000 ? value[..2000] : value;
}
