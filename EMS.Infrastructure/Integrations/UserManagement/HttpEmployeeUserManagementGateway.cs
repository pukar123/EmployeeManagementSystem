using System.Net;
using EMS.Application.Services.Employees;
using EMS.Application.Services.Integrations;
using Microsoft.Extensions.Logging;
using Pukar.Usermanagement.Contracts.Users;

namespace EMS.Infrastructure.Integrations.UserManagement;

public sealed class HttpEmployeeUserManagementGateway : IEmployeeUserManagementGateway
{
    private readonly IUserManagementHttpClient _http;
    private readonly ILogger<HttpEmployeeUserManagementGateway> _logger;

    public HttpEmployeeUserManagementGateway(
        IUserManagementHttpClient http,
        ILogger<HttpEmployeeUserManagementGateway> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<EmployeeLinkedUserSnapshot?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var users = await GetUsersByIdsAsync(new[] { userId }, cancellationToken);
        return users.TryGetValue(userId, out var user) ? user : null;
    }

    public async Task<EmployeeLinkedUserSnapshot?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _http.GetAsync<UserSummaryResponseModel>(
                $"/api/internal/v1/users/by-email?email={Uri.EscapeDataString(email)}",
                cancellationToken,
                allowRetry: true);
            return user is null ? null : ToSnapshot(user);
        }
        catch (UserManagementHttpException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (UserManagementDependencyUnavailableException ex)
        {
            _logger.LogWarning(ex, "User Management unavailable while resolving user by email.");
            return null;
        }
    }

    public async Task<IReadOnlyDictionary<int, EmployeeLinkedUserSnapshot>> GetUsersByIdsAsync(
        IReadOnlyList<int> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<int, EmployeeLinkedUserSnapshot>();

        try
        {
            var users = await _http.PostAsync<List<UserSummaryResponseModel>>(
                "/api/internal/v1/users/lookup",
                new BatchUserLookupRequestModel { UserIds = userIds.ToList() },
                cancellationToken,
                allowRetry: true);

            return (users ?? [])
                .Select(ToSnapshot)
                .ToDictionary(u => u.Id);
        }
        catch (UserManagementDependencyUnavailableException ex)
        {
            _logger.LogWarning(ex, "User Management unavailable while looking up users. Returning empty enrichment.");
            return new Dictionary<int, EmployeeLinkedUserSnapshot>();
        }
    }

    public async Task<IReadOnlyList<string>> GetRoleKeysForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync<UserRoleKeysResponseModel>(
            $"/api/internal/v1/users/{userId}/roles",
            cancellationToken,
            allowRetry: true);

        return response?.NormalizedRoleKeys ?? Array.Empty<string>();
    }

    public Task SetRoleKeysForUserAsync(
        int userId,
        IReadOnlyList<string> normalizedRoleKeys,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        => _http.PutAsync(
            $"/api/internal/v1/users/{userId}/roles",
            new ReplaceUserRolesByKeysRequestModel
            {
                NormalizedRoleKeys = normalizedRoleKeys
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Select(k => k.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList(),
            },
            cancellationToken,
            idempotencyKey);

    public Task DeactivateLinkedUserAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        => _http.PostAsync(
            $"/api/internal/v1/users/{userId}/deactivate",
            body: null,
            cancellationToken,
            allowRetry: false,
            idempotencyKey);

    public Task RevokeOperationalAccessAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        => SetRoleKeysForUserAsync(userId, Array.Empty<string>(), idempotencyKey, cancellationToken);

    public Task ActivateLinkedUserAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        => _http.PostAsync(
            $"/api/internal/v1/users/{userId}/activate",
            body: null,
            cancellationToken,
            allowRetry: false,
            idempotencyKey);

    public Task RevokeSessionsAsync(
        int userId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
        => _http.PostAsync(
            $"/api/internal/v1/users/{userId}/revoke-sessions",
            body: null,
            cancellationToken,
            allowRetry: false,
            idempotencyKey);

    private static EmployeeLinkedUserSnapshot ToSnapshot(UserSummaryResponseModel user)
        => new()
        {
            Id = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
        };
}
