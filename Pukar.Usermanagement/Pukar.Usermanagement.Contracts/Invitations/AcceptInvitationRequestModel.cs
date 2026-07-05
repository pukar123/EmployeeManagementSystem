namespace Pukar.Usermanagement.Contracts.Invitations;

public sealed class AcceptInvitationRequestModel
{
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
