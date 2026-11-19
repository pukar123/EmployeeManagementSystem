using Pukar.Usermanagement.Application.Services.Email;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// Replaces the SMTP sender so no test ever tries to send real email.
/// </summary>
internal sealed class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
