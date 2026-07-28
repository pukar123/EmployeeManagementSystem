namespace Pukar.Usermanagement.Contracts.Invitations;

public sealed class CreateInvitationRequestModel
{
    public string ExternalCorrelationId { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;

    public string? RecipientDisplayName { get; set; }

    /// <summary>
    /// Explicit account-linking workflow: link invitation to an existing inactive, never-logged-in UM user.
    /// Must match the recipient email. Omit when provisioning a brand-new account.
    /// </summary>
    public int? LinkExistingInactiveUserId { get; set; }
}
