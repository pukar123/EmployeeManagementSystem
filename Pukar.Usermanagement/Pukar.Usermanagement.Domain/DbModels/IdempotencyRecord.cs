namespace Pukar.Usermanagement.Domain.DbModels;

public class IdempotencyRecord
{
    public long Id { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;

    public string RequestFingerprint { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string? ResponseBody { get; set; }

    public string? ContentType { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
}
