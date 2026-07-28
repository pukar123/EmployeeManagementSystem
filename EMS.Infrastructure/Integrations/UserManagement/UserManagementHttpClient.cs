using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Integrations;
using Microsoft.Extensions.Logging;
using Pukar.Shared;
using Pukar.Usermanagement.Contracts.ServiceAuth;

namespace EMS.Infrastructure.Integrations.UserManagement;

public interface IUserManagementHttpClient
{
    Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default, bool allowRetry = true);

    Task<T?> PostAsync<T>(
        string path,
        object? body,
        CancellationToken cancellationToken = default,
        bool allowRetry = false,
        string? idempotencyKey = null);

    Task PostAsync(
        string path,
        object? body,
        CancellationToken cancellationToken = default,
        bool allowRetry = false,
        string? idempotencyKey = null);

    Task PutAsync(
        string path,
        object? body,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null);

    Task DeleteAsync(
        string path,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null);
}

public sealed class UserManagementHttpClient : IUserManagementHttpClient
{
    public const string CorrelationHeaderName = "X-Correlation-ID";
    public const string IdempotencyHeaderName = "Idempotency-Key";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserManagementServiceTokenProvider _tokenProvider;
    private readonly IAuditContextAccessor _auditContext;
    private readonly ILogger<UserManagementHttpClient> _logger;

    public UserManagementHttpClient(
        IHttpClientFactory httpClientFactory,
        IUserManagementServiceTokenProvider tokenProvider,
        IAuditContextAccessor auditContext,
        ILogger<UserManagementHttpClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenProvider = tokenProvider;
        _auditContext = auditContext;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default, bool allowRetry = true)
        => SendAsync<T>(HttpMethod.Get, path, body: null, cancellationToken, allowRetry, idempotencyKey: null);

    public Task<T?> PostAsync<T>(
        string path,
        object? body,
        CancellationToken cancellationToken = default,
        bool allowRetry = false,
        string? idempotencyKey = null)
        => SendAsync<T>(HttpMethod.Post, path, body, cancellationToken, allowRetry, idempotencyKey);

    public async Task PostAsync(
        string path,
        object? body,
        CancellationToken cancellationToken = default,
        bool allowRetry = false,
        string? idempotencyKey = null)
    {
        await SendAsync<object>(HttpMethod.Post, path, body, cancellationToken, allowRetry, idempotencyKey, expectBody: false);
    }

    public async Task PutAsync(
        string path,
        object? body,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null)
    {
        await SendAsync<object>(HttpMethod.Put, path, body, cancellationToken, allowRetry: false, idempotencyKey, expectBody: false);
    }

    public async Task DeleteAsync(
        string path,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null)
    {
        await SendAsync<object>(HttpMethod.Delete, path, body: null, cancellationToken, allowRetry: false, idempotencyKey, expectBody: false);
    }

    private async Task<T?> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken,
        bool allowRetry,
        string? idempotencyKey,
        bool expectBody = true)
    {
        try
        {
            var clientName = allowRetry ? UserManagementHttpClientNames.SafeRetry : UserManagementHttpClientNames.Default;
            var client = _httpClientFactory.CreateClient(clientName);
            using var request = new HttpRequestMessage(method, path);

            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            ApplyAuditHeaders(request);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
                request.Headers.TryAddWithoutValidation(IdempotencyHeaderName, idempotencyKey);

            if (body is not null)
                request.Content = JsonContent.Create(body, options: JsonOptions);

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                if (!expectBody || response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(responseBody))
                    return default;

                return JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
            }

            throw MapStatus(response.StatusCode, responseBody);
        }
        catch (UserManagementDependencyUnavailableException)
        {
            throw;
        }
        catch (UserManagementHttpException ex) when (ex.IsDependencyFailure)
        {
            throw new UserManagementDependencyUnavailableException(
                "User Management is currently unavailable.",
                ex)
            {
                StatusCode = (int)ex.StatusCode,
            };
        }
        catch (UserManagementHttpException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new UserManagementDependencyUnavailableException("User Management request timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "User Management HTTP transport failure for {Method} {Path}", method, path);
            throw new UserManagementDependencyUnavailableException("User Management is currently unavailable.", ex);
        }
    }

    private void ApplyAuditHeaders(HttpRequestMessage request)
    {
        var correlationId = _auditContext.GetCorrelationId();
        if (!string.IsNullOrWhiteSpace(correlationId))
            request.Headers.TryAddWithoutValidation(CorrelationHeaderName, correlationId);

        var user = _auditContext.GetCurrentUser();
        if (user.UserId is int userId)
            request.Headers.TryAddWithoutValidation(AuditHeaders.InitiatingUserId, userId.ToString());

        if (!string.IsNullOrWhiteSpace(user.Email))
            request.Headers.TryAddWithoutValidation(AuditHeaders.InitiatingUserEmail, user.Email);
    }

    private static Exception MapStatus(HttpStatusCode statusCode, string responseBody)
    {
        var message = TryReadMessage(responseBody) ?? $"User Management returned {(int)statusCode}.";

        return statusCode switch
        {
            HttpStatusCode.BadRequest => new BusinessRuleException(message),
            HttpStatusCode.NotFound => new UserManagementHttpException(statusCode, message, responseBody),
            HttpStatusCode.Conflict => new BusinessRuleException(message),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                or HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout
                => new UserManagementHttpException(statusCode, message, responseBody),
            >= HttpStatusCode.InternalServerError
                => new UserManagementHttpException(statusCode, message, responseBody),
            _ => new UserManagementHttpException(statusCode, message, responseBody),
        };
    }

    private static string? TryReadMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("message", out var messageProp)
                && messageProp.ValueKind == JsonValueKind.String)
            {
                return messageProp.GetString();
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return responseBody.Length > 500 ? responseBody[..500] : responseBody;
    }
}
