using EMS.API.Options;
using Microsoft.Extensions.Options;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace EMS.API.Bootstrap;

/// <summary>
/// Ensures a single default admin user exists when configured (development / first run).
/// </summary>
public sealed class AdminUserSeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminUserSeedHostedService> _logger;

    public AdminUserSeedHostedService(IServiceProvider serviceProvider, ILogger<AdminUserSeedHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<SeedAdminOptions>>().Value;
            if (!options.Enabled || string.IsNullOrWhiteSpace(options.Password))
            {
                _logger.LogInformation("Seed admin skipped (SeedAdmin:Enabled=false or Password empty).");
                return;
            }

            var email = options.Email.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Seed admin skipped: SeedAdmin:Email is empty.");
                return;
            }

            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var normalized = EmailNormalizer.Normalize(email);
            if (await users.GetByNormalizedEmailAsync(normalized, cancellationToken) is not null)
            {
                _logger.LogInformation("Seed admin skipped: user with normalized email already exists.");
                return;
            }

            var utcNow = DateTime.UtcNow;
            var user = new User
            {
                Email = email,
                NormalizedEmail = normalized,
                PasswordHash = passwordHasher.HashPassword(options.Password),
                UserName = "Administrator",
                IsActive = true,
                CreatedAtUtc = utcNow,
            };

            await users.AddAsync(user, cancellationToken);
            await users.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded default admin user for email {Email}.", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed default admin user. Ensure UserManagement migrations are applied.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
