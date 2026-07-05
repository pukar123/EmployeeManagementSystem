using System.Text.Json;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Integrations;

public interface IIntegrationOutboxWriter
{
    Task EnqueueRevokeLinkedIdentityAsync(int employeeId, CancellationToken cancellationToken = default);

    Task EnqueueSyncEmployeeRolesAsync(int employeeId, CancellationToken cancellationToken = default);
}

public sealed class IntegrationOutboxWriter : IIntegrationOutboxWriter
{
    private readonly IBaseRepository<IntegrationOutboxMessage> _outbox;

    public IntegrationOutboxWriter(IBaseRepository<IntegrationOutboxMessage> outbox)
    {
        _outbox = outbox;
    }

    public Task EnqueueRevokeLinkedIdentityAsync(int employeeId, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            IntegrationOutboxMessageType.RevokeEmployeeLinkedIdentity,
            $"revoke-identity:employee:{employeeId}",
            new { employeeId },
            cancellationToken);

    public Task EnqueueSyncEmployeeRolesAsync(int employeeId, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            IntegrationOutboxMessageType.SyncEmployeeRoles,
            $"sync-roles:employee:{employeeId}",
            new { employeeId },
            cancellationToken);

    private async Task EnqueueAsync(
        IntegrationOutboxMessageType type,
        string idempotencyKey,
        object payload,
        CancellationToken cancellationToken)
    {
        var hasPending = await _outbox.GetQueryable()
            .AnyAsync(
                m => m.MessageType == type
                    && m.IdempotencyKey == idempotencyKey
                    && (m.Status == IntegrationOutboxStatus.Pending || m.Status == IntegrationOutboxStatus.Processing),
                cancellationToken);

        if (hasPending)
            return;

        await _outbox.AddAsync(
            new IntegrationOutboxMessage
            {
                MessageType = type,
                IdempotencyKey = idempotencyKey,
                PayloadJson = JsonSerializer.Serialize(payload),
                Status = IntegrationOutboxStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                NextAttemptAtUtc = DateTime.UtcNow,
            },
            cancellationToken);
    }
}

public interface IIntegrationOutboxProcessor
{
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
}
