using Microsoft.Extensions.Options;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Contracts.ServiceAuth;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Host.Bootstrap;

public sealed class ServiceClientSeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServiceClientSeedHostedService> _logger;

    public ServiceClientSeedHostedService(IServiceProvider serviceProvider, ILogger<ServiceClientSeedHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
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
