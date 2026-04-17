namespace EMS.Domain.DbModels;

public class AuditTrailEntry
{
    public long Id { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public string EntityKey { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public DateTime ChangedAtUtc { get; set; }

    public int? ActorUserId { get; set; }

    public string? ActorEmail { get; set; }

    public string? ActorUserName { get; set; }

    public string? CorrelationId { get; set; }
}
