using Pukar.Usermanagement.Domain.Enums;

namespace Pukar.Usermanagement.Domain.DbModels;

public class AccountInvitation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ExternalCorrelationId { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;

    public string? RecipientDisplayName { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastSentAtUtc { get; set; }

    public AccountInvitationDeliveryStatus DeliveryStatus { get; set; } = AccountInvitationDeliveryStatus.Pending;

    public string? DeliveryFailureReason { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public User User { get; set; } = null!;
}
