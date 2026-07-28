using System.Collections.Concurrent;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Services.Jwt;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Contracts.ServiceAuth;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Application.Services.ServiceAuth;

public sealed class ServiceAuthService : IServiceAuthService
{
    private static readonly ConcurrentDictionary<string, TokenRateLimitState> RateLimits = new(StringComparer.Ordinal);

    private readonly IServiceClientRepository _clients;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;

    public ServiceAuthService(
        IServiceClientRepository clients,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt)
    {
        _clients = clients;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
    }

    public async Task<ServiceTokenResponseModel> IssueTokenAsync(
        ServiceTokenRequestModel request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
            throw new BusinessRuleException("Client id and secret are required.");

        EnforceRateLimit(request.ClientId.Trim());

        var client = await _clients.GetByClientIdAsync(request.ClientId.Trim(), cancellationToken);
        if (client is null || !client.IsActive || !_passwordHasher.VerifyPassword(request.ClientSecret, client.SecretHash))
            throw new UnauthorizedServiceException("Invalid client credentials.");

        var scopes = client.AllowedScopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var utcNow = DateTime.UtcNow;
        var token = _jwt.CreateServiceToken(client.ClientId, scopes, utcNow, out var expiresAtUtc);

        return new ServiceTokenResponseModel
        {
            AccessToken = token,
            ExpiresInSeconds = (int)Math.Max(1, (expiresAtUtc - utcNow).TotalSeconds),
            Scopes = scopes,
        };
    }

    private static void EnforceRateLimit(string clientId)
    {
        var now = DateTime.UtcNow;
        var state = RateLimits.GetOrAdd(clientId, _ => new TokenRateLimitState());
        lock (state)
        {
            state.Timestamps.RemoveAll(t => now - t > TimeSpan.FromMinutes(1));
            if (state.Timestamps.Count >= 30)
                throw new BusinessRuleException("Service token rate limit exceeded. Retry later.");

            state.Timestamps.Add(now);
        }
    }

    private sealed class TokenRateLimitState
    {
        public List<DateTime> Timestamps { get; } = [];
    }
}
