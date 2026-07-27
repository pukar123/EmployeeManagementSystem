using EMS.API.Options;
using Microsoft.Extensions.Options;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Contracts.Roles;
using Pukar.Usermanagement.Contracts.ServiceAuth;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace EMS.API.Bootstrap;

/// <summary>
/// Consolidated-host seed orchestration for the User Management bounded context.
/// The trigger lives in EMS.API (the composition root) while all business operations run
/// through UM repositories/services. Seeds the system Admin role, an opt-in admin user, and
/// the EMS service client (used only by the retained internal compatibility endpoints).
/// </summary>
public sealed class UserManagementSeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserManagementSeedHostedService> _logger;

    public UserManagementSeedHostedService(IServiceProvider serviceProvider, ILogger<UserManagementSeedHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SeedAdminAsync(cancellationToken);
        await SeedServiceClientAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
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
            else if (options.ResetExistingPassword)
            {
                user.PasswordHash = passwordHasher.HashPassword(options.Password);
                user.IsActive = true;
                user.MustChangePassword = false;
                users.Update(user);
                await users.SaveChangesAsync(cancellationToken);
                _logger.LogWarning("Reset seeded admin user password for email {Email} (SeedAdmin:ResetExistingPassword=true).", email);
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

    private async Task SeedServiceClientAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<ServiceClientsOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.Ems.Secret))
            {
                _logger.LogInformation("EMS service client seed skipped (ServiceClients:Ems:Secret empty).");
                return;
            }

            var clients = scope.ServiceProvider.GetRequiredService<IServiceClientRepository>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var clientId = options.Ems.ClientId.Trim();
            var existing = await clients.GetByClientIdAsync(clientId, cancellationToken);
            var scopes = string.Join(
                ' ',
                ServiceScopes.UsersRead,
                ServiceScopes.UsersManage,
                ServiceScopes.RolesRead,
                ServiceScopes.RolesManage,
                ServiceScopes.InvitationsManage);

            if (existing is null)
            {
                await clients.AddAsync(
                    new ServiceClient
                    {
                        ClientId = clientId,
                        Name = options.Ems.Name,
                        SecretHash = hasher.HashPassword(options.Ems.Secret),
                        AllowedScopes = scopes,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                    },
                    cancellationToken);
                await clients.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Seeded service client {ClientId}.", clientId);
                return;
            }

            var updated = false;
            if (!hasher.VerifyPassword(options.Ems.Secret, existing.SecretHash))
            {
                existing.SecretHash = hasher.HashPassword(options.Ems.Secret);
                updated = true;
                _logger.LogWarning(
                    "Rotated secret for service client {ClientId} from configuration. Treat any prior secret as compromised.",
                    clientId);
            }

            if (!string.Equals(existing.AllowedScopes, scopes, StringComparison.Ordinal))
            {
                existing.AllowedScopes = scopes;
                updated = true;
            }

            if (updated)
            {
                clients.Update(existing);
                await clients.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed service clients.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
