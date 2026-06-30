using System.Text.Json;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Integrations;

public sealed class IntegrationOutboxProcessor : IIntegrationOutboxProcessor
{
    private readonly IBaseRepository<IntegrationOutboxMessage> _outbox;
    private readonly IBaseRepository<Employee> _employees;
    private readonly IEmployeeUserManagementGateway _gateway;
    private readonly IEmployeeRoleSyncService _roleSyncService;

    public IntegrationOutboxProcessor(
        IBaseRepository<IntegrationOutboxMessage> outbox,
        IBaseRepository<Employee> employees,
        IEmployeeUserManagementGateway gateway,
        IEmployeeRoleSyncService roleSyncService)
    {
        _outbox = outbox;
        _employees = employees;
        _gateway = gateway;
        _roleSyncService = roleSyncService;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pending = await _outbox.GetQueryable()
            .Where(m => m.Status == IntegrationOutboxStatus.Pending && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now))
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
            catch (Exception ex)
            {
                message.Status = IntegrationOutboxStatus.Failed;
                message.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                message.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(Math.Min(message.AttemptCount * 5, 60));
                message.Status = IntegrationOutboxStatus.Pending;
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
                await EmployeeLinkedIdentityHelper.RevokeLinkedIdentityAsync(employee, _gateway, cancellationToken);
                break;
            case IntegrationOutboxMessageType.SyncEmployeeRoles:
                await _roleSyncService.SyncEmployeeAsync(employeeId, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unknown outbox message type {message.MessageType}.");
        }
    }
}
