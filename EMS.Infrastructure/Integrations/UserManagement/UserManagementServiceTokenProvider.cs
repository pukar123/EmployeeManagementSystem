using System.Net.Http.Json;
using System.Text.Json;
using EMS.Application.Options;
using EMS.Application.Services.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pukar.Usermanagement.Contracts.ServiceAuth;

namespace EMS.Infrastructure.Integrations.UserManagement;

public interface IUserManagementServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class UserManagementServiceTokenProvider : IUserManagementServiceTokenProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UserManagementApiOptions _options;
    private readonly ILogger<UserManagementServiceTokenProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAtUtc = DateTimeOffset.MinValue;

    public UserManagementServiceTokenProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<UserManagementApiOptions> options,
        ILogger<UserManagementServiceTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _expiresAtUtc)
            return _cachedToken;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _expiresAtUtc)
                return _cachedToken;

            EnsureConfigured();

            var client = _httpClientFactory.CreateClient(UserManagementHttpClientNames.Default);
            using var response = await client.PostAsJsonAsync(
                "/api/internal/v1/service-token",
                new ServiceTokenRequestModel
                {
                    ClientId = _options.ServiceClientId,
                    ClientSecret = _options.ServiceClientSecret,
                },
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to obtain User Management service token. Status={StatusCode} Body={Body}",
                    (int)response.StatusCode,
                    body);

                throw new UserManagementDependencyUnavailableException(
                    "User Management service token could not be obtained.",
                    new UserManagementHttpException(response.StatusCode, "Service token request failed.", body))
                {
                    StatusCode = (int)response.StatusCode,
                };
            }

            var token = JsonSerializer.Deserialize<ServiceTokenResponseModel>(body, JsonOptions)
                ?? throw new UserManagementDependencyUnavailableException("User Management returned an empty service token response.");

            if (string.IsNullOrWhiteSpace(token.AccessToken))
                throw new UserManagementDependencyUnavailableException("User Management returned an empty access token.");

            var skewSeconds = Math.Min(30, Math.Max(5, token.ExpiresInSeconds / 10));
            _cachedToken = token.AccessToken;
            _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, token.ExpiresInSeconds - skewSeconds));
            return _cachedToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new UserManagementDependencyUnavailableException("UserManagementApi:BaseUrl is not configured.");

        if (string.IsNullOrWhiteSpace(_options.ServiceClientId) || string.IsNullOrWhiteSpace(_options.ServiceClientSecret))
            throw new UserManagementDependencyUnavailableException("UserManagementApi service client credentials are not configured.");
    }
}
