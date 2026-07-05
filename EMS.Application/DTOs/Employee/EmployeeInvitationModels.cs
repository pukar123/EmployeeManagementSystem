using EMS.Domain.Enums;

namespace EMS.Application.DTOs.Employee;

public sealed class EmployeeInvitationResponseModel
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public DateTime? LastSentAtUtc { get; set; }

    public EmployeeInvitationDeliveryStatus DeliveryStatus { get; set; }

    public string? DeliveryFailureReason { get; set; }

    public bool CanResend { get; set; }

    public int ResendCooldownSecondsRemaining { get; set; }
}
