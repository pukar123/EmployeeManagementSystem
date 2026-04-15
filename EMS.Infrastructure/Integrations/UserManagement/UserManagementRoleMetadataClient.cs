using System.Net.Http.Json;
using EMS.Application.Services.Authorization;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Integrations.UserManagement;

public sealed class UserManagementRoleMetadataClient : IUserManagementRoleMetadataClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserManagementRoleMetadataClient> _logger;

    public UserManagementRoleMetadataClient(HttpClient httpClient, ILogger<UserManagementRoleMetadataClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoleMetadataSnapshot>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        if (_httpClient.BaseAddress is null)
        {
            _logger.LogDebug("UserManagement role metadata client disabled: BaseAddress is not configured.");
            return Array.Empty<RoleMetadataSnapshot>();
        }

        try
        {
            var payload = await _httpClient.GetFromJsonAsync<List<RoleMetadataSnapshot>>(string.Empty, cancellationToken);
            if (payload is null)
                return Array.Empty<RoleMetadataSnapshot>();

            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch UserManagement role metadata.");
            return Array.Empty<RoleMetadataSnapshot>();
        }
    }
}
