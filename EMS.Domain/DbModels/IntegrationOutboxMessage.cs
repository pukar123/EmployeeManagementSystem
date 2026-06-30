using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class IntegrationOutboxMessage
{
    public int Id { get; set; }

    public IntegrationOutboxMessageType MessageType { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public IntegrationOutboxStatus Status { get; set; } = IntegrationOutboxStatus.Pending;

    public int AttemptCount { get; set; }

    public DateTime? NextAttemptAtUtc { get; set; }

    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }
}
