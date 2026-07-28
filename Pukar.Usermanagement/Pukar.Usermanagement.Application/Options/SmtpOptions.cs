namespace Pukar.Usermanagement.Application.Options;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = "noreply@localhost";

    public string SenderName { get; set; } = "User Management";

    public string WebAppBaseUrl { get; set; } = "http://localhost:3000";
}
