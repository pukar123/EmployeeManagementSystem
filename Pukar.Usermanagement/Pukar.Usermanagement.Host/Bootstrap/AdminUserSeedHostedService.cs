using Microsoft.Extensions.Options;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Contracts.Roles;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;
using Pukar.Usermanagement.Host.Options;

namespace Pukar.Usermanagement.Host.Bootstrap;

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
            var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var userRoles = scope.ServiceProvider.GetRequiredService<IUserRoleRepository>();

            var adminRole = await roles.GetByNormalizedNameAsync(WellKnownRoles.AdminNormalizedName, cancellationToken);
            if (adminRole is null)
            {
                adminRole = new Role
                {
                    Name = WellKnownRoles.Admin,
                    NormalizedName = WellKnownRoles.AdminNormalizedName,
                    Description = "Full access (system role).",
                    IsSystem = true,
                };
                await roles.AddAsync(adminRole, cancellationToken);
                await roles.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Seeded system role {Role}.", WellKnownRoles.Admin);
            }

            var options = scope.ServiceProvider.GetRequiredService<IOptions<SeedAdminOptions>>().Value;
            if (!options.Enabled || string.IsNullOrWhiteSpace(options.Password))
            {
                _logger.LogInformation("Seed admin user skipped (SeedAdmin:Enabled=false or Password empty).");
                return;
            }

            var email = options.Email.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Seed admin skipped: SeedAdmin:Email is empty.");
                return;
            }

            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var normalized = EmailNormalizer.Normalize(email);
            var user = await users.GetByNormalizedEmailAsync(normalized, cancellationToken);
            if (user is null)
            {
                var utcNow = DateTime.UtcNow;
                user = new User
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

            var roleIds = await userRoles.GetRoleIdsForUserAsync(user.Id, cancellationToken);
            if (!roleIds.Contains(adminRole.Id))
            {
                var merged = roleIds.Concat(new[] { adminRole.Id }).Distinct().ToList();
                await userRoles.ReplaceRolesForUserAsync(user.Id, merged, cancellationToken);
                _logger.LogInformation("Assigned Admin role to user id {UserId}.", user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed admin role/user. Ensure UserManagement migrations are applied.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
