using System.Net.Http.Headers;
using EMS.API.Bootstrap;
using EMS.Domain.Database;
using EMS.Infrastructure.Persistence.Auditing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Email;
using Pukar.Usermanagement.Application.Services.Jwt;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Infrastructure.Services;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// Starts the real EMS.API in memory for access tests.
/// Sign-in, the global "must be signed in" rule, the AdminAccess policy and the EMS access services
/// are the production registrations. Only these are swapped: both databases (EF Core InMemory),
/// the background/seed hosted services, Mongo logging (turned off by setting) and the email sender.
/// </summary>
public sealed class EmsApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "Pukar.Usermanagement";
    private const string Audience = "ems";

    // Test-only signing keys. They are not real credentials and are only used by this in-memory host.
    private const string TestSigningKey = "ems-integration-tests-only-signing-key-not-a-secret";
    private const string UntrustedSigningKey = "a-different-key-the-ems-api-does-not-trust-0000";

    private static readonly Type[] HostedServicesToRemove =
    [
        typeof(EmsRbacSeedHostedService),
        typeof(RoleKeyBackfillHostedService),
        typeof(EmployeeScheduledChangeWorker),
        typeof(ScheduledChangeReminderWorker),
        typeof(DocumentExpiryNotificationWorker),
        typeof(IntegrationOutboxDispatcher),
        typeof(UserManagementSeedHostedService),
        typeof(UserManagementIdempotencyCleanupHostedService),
    ];

    private readonly string _appDatabaseName = $"ems-access-tests-{Guid.NewGuid():N}";
    private readonly string _userManagementDatabaseName = $"um-access-tests-{Guid.NewGuid():N}";

    /// <summary>Starts the host and seeds <see cref="TestSeed"/>. Call once per fixture.</summary>
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        await TestSeed.SeedAsync(Services, cancellationToken);
    }

    /// <summary>A client that sends whatever token <paramref name="user"/> calls for.</summary>
    public HttpClient CreateClientAs(TestUser user)
    {
        var client = CreateAnonymousClient();
        var token = user.Token switch
        {
            TestTokenKind.None => null,
            TestTokenKind.Trusted => CreateTrustedAccessToken(user),
            TestTokenKind.UntrustedSigningKey => CreateUntrustedAccessToken(user),
            _ => throw new ArgumentOutOfRangeException(nameof(user), user.Token, "Unknown token kind."),
        };

        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>A client that sends no token.</summary>
    public HttpClient CreateAnonymousClient()
        => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Host settings are visible to Program.cs while it registers services, which matters because
        // AddUserManagementJwtAuthentication reads the Jwt section straight away.
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
        builder.UseSetting("ConnectionStrings:MongoLogs", string.Empty);
        builder.UseSetting("SeedAdmin:Enabled", "false");
        builder.UseSetting("Smtp:Host", string.Empty);

        builder.ConfigureTestServices(services =>
        {
            RemoveHostedServices(services);
            UseInMemoryDatabases(services);

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        });
    }

    private static void RemoveHostedServices(IServiceCollection services)
    {
        // Remove by implementation type only: the framework's own web host service must stay.
        var toRemove = services
            .Where(d => d.ServiceType == typeof(IHostedService)
                        && d.ImplementationType is not null
                        && HostedServicesToRemove.Contains(d.ImplementationType))
            .ToList();

        foreach (var descriptor in toRemove)
            services.Remove(descriptor);
    }

    private void UseInMemoryDatabases(IServiceCollection services)
    {
        // Both the options and EF Core 9's options-configuration registrations must go, or EF Core
        // reports that two database providers are registered.
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
        services.RemoveAll<DbContextOptions<UserManagementDbContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<UserManagementDbContext>>();

        services.AddDbContext<AppDbContext>((sp, options) => options
            .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>())
            .UseInMemoryDatabase(_appDatabaseName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddDbContext<UserManagementDbContext>(options => options
            .UseInMemoryDatabase(_userManagementDatabaseName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
    }

    private string CreateTrustedAccessToken(TestUser user)
    {
        using var scope = Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return tokens.CreateAccessToken(
            user.UserId, user.Email, user.Description, user.RoleNames, DateTime.UtcNow, out _);
    }

    private static string CreateUntrustedAccessToken(TestUser user)
    {
        // Same issuing code, issuer and audience as the API, but a signing key the API does not know.
        var untrusted = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(new JwtTokenOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = UntrustedSigningKey,
        }));
        return untrusted.CreateAccessToken(
            user.UserId, user.Email, user.Description, user.RoleNames, DateTime.UtcNow, out _);
    }
}
