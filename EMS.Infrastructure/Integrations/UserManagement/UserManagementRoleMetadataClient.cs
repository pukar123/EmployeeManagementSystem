using EMS.Application.Options;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pukar.Usermanagement.Contracts.Roles;

namespace EMS.Infrastructure.Integrations.UserManagement;

public sealed class UserManagementRoleMetadataClient : IUserManagementRoleMetadataClient
{
    private readonly IUserManagementHttpClient _http;
    private readonly UserManagementApiOptions _options;
    private readonly ILogger<UserManagementRoleMetadataClient> _logger;
    private readonly SemaphoreSlim _cacheGate = new(1, 1);

    private IReadOnlyList<RoleMetadataSnapshot>? _cache;
    private DateTimeOffset _cacheExpiresAtUtc = DateTimeOffset.MinValue;

    public UserManagementRoleMetadataClient(
        IUserManagementHttpClient http,
        IOptions<UserManagementApiOptions> options,
        ILogger<UserManagementRoleMetadataClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoleMetadataSnapshot>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _logger.LogDebug("UserManagement role metadata client disabled: BaseUrl is not configured.");
            return Array.Empty<RoleMetadataSnapshot>();
        }

        if (_cache is not null && DateTimeOffset.UtcNow < _cacheExpiresAtUtc)
            return _cache;

        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null && DateTimeOffset.UtcNow < _cacheExpiresAtUtc)
                return _cache;

            var path = string.IsNullOrWhiteSpace(_options.RolesMetadataPath)
                ? "/api/internal/v1/roles/metadata"
                : _options.RolesMetadataPath;

            var payload = await _http.GetAsync<List<RoleMetadataV1ResponseModel>>(path, cancellationToken, allowRetry: true);
            _cache = (payload ?? [])
                .Select(r => new RoleMetadataSnapshot
                {
                    Id = r.Id,
                    Name = r.Name,
                    NormalizedName = r.NormalizedName,
                    IsSystem = r.IsSystem,
                })
                .ToList();
            _cacheExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5);
            return _cache;
        }
        catch (UserManagementDependencyUnavailableException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch UserManagement role metadata.");
            return _cache ?? Array.Empty<RoleMetadataSnapshot>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch UserManagement role metadata.");
            return _cache ?? Array.Empty<RoleMetadataSnapshot>();
        }
        finally
        {
            _cacheGate.Release();
        }
    }
}
