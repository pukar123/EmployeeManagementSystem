namespace Pukar.Usermanagement.Contracts.Invitations;

public sealed class InvitationResponseModel
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ExternalCorrelationId { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public DateTime? LastSentAtUtc { get; set; }

    public InvitationDeliveryStatus DeliveryStatus { get; set; }

    public string? DeliveryFailureReason { get; set; }

    public bool CanResend { get; set; }

    public int ResendCooldownSecondsRemaining { get; set; }
}
