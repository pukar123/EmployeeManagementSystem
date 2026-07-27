namespace Pukar.Usermanagement.Application.Options;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public int TokenExpirationMinutes { get; set; } = 30;

    public int RequestCooldownSeconds { get; set; } = 60;
}
