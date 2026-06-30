using System.Text.Json;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;

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
        => EnqueueAsync(IntegrationOutboxMessageType.RevokeEmployeeLinkedIdentity, new { employeeId }, cancellationToken);

    public Task EnqueueSyncEmployeeRolesAsync(int employeeId, CancellationToken cancellationToken = default)
        => EnqueueAsync(IntegrationOutboxMessageType.SyncEmployeeRoles, new { employeeId }, cancellationToken);

    private async Task EnqueueAsync(
        IntegrationOutboxMessageType type,
        object payload,
        CancellationToken cancellationToken)
    {
        await _outbox.AddAsync(
            new IntegrationOutboxMessage
            {
                MessageType = type,
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
