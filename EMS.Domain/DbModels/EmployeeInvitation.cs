using EMS.Domain.Enums;

namespace EMS.Domain.DbModels;

public class EmployeeInvitation
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public DateTime? LastSentAtUtc { get; set; }

    public EmployeeInvitationDeliveryStatus DeliveryStatus { get; set; } = EmployeeInvitationDeliveryStatus.Pending;

    public string? DeliveryFailureReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Employee Employee { get; set; } = null!;
}
