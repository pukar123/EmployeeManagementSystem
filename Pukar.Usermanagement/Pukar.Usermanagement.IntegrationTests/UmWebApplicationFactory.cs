using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pukar.Usermanagement.Application.Services.Email;
using Pukar.Usermanagement.Domain.Database;

namespace Pukar.Usermanagement.IntegrationTests;

public sealed class UmWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"um-tests-{Guid.NewGuid()}";
    private readonly IReadOnlyDictionary<string, string?> _settings;

    /// <param name="settings">
    /// Extra configuration for this host only, applied after the defaults below
    /// (e.g. <c>Registration:AllowSelfRegistration</c> or low <c>AuthRateLimiting</c> limits).
    /// </param>
    public UmWebApplicationFactory(IReadOnlyDictionary<string, string?>? settings = null)
    {
        _settings = settings ?? new Dictionary<string, string?>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:UserManagementDb"] = "InMemory",
                ["Jwt:Issuer"] = "Pukar.Usermanagement",
                ["Jwt:Audience"] = "ems",
                ["Jwt:SigningKeyId"] = "test-key",
                ["Jwt:SigningKey"] = "Integration_Test_Signing_Key_32Chars!!",
                ["ServiceClients:Ems:ClientId"] = "ems",
                ["ServiceClients:Ems:Secret"] = "test-secret",
                ["ServiceClients:Ems:Name"] = "EMS Test",
                ["SeedAdmin:Enabled"] = "false",
                ["Smtp:Host"] = "",
            });
            config.AddInMemoryCollection(_settings);
        });

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<UserManagementDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        });
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
