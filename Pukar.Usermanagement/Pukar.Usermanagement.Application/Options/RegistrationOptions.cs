namespace Pukar.Usermanagement.Application.Options;

/// <summary>
/// Controls public self-registration (<c>POST /api/auth/register</c>).
/// Off by default: accounts are created through invitations or by an administrator.
/// </summary>
public sealed class RegistrationOptions
{
    public const string SectionName = "Registration";

    public const string DisabledCode = "registration_disabled";

    public const string DisabledMessage =
        "Self-registration is not available. Ask your administrator for an invitation.";

    public bool AllowSelfRegistration { get; set; }
}
