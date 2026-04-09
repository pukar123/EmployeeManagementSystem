using Microsoft.Extensions.Options;
using Pukar.Usermanagement.Application.DTOs.Auth;
using Pukar.Usermanagement.Application.Helpers;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Jwt;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Shared;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.Application.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly JwtTokenOptions _jwtOptions;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        IOptions<JwtTokenOptions> jwtOptions)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponseModel> RegisterAsync(
        RegisterRequestModel request,
        string? clientInfo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new BusinessRuleException("Email and password are required.");

        var normalized = EmailNormalizer.Normalize(request.Email);
        if (await _users.GetByNormalizedEmailAsync(normalized, cancellationToken) is not null)
            throw new DuplicateEmailException();

        var utcNow = DateTime.UtcNow;
        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalized,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            UserName = string.IsNullOrWhiteSpace(request.UserName) ? null : request.UserName.Trim(),
            IsActive = true,
            CreatedAtUtc = utcNow,
        };

        await _users.AddAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, clientInfo, utcNow, cancellationToken);
    }

    public async Task<AuthResponseModel> LoginAsync(
        LoginRequestModel request,
        string? clientInfo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new BusinessRuleException("Email and password are required.");

        var normalized = EmailNormalizer.Normalize(request.Email);
        var user = await _users.GetByNormalizedEmailAsync(normalized, cancellationToken);
        if (user is null || !user.IsActive)
            throw new BusinessRuleException("Invalid email or password.");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new BusinessRuleException("Invalid email or password.");

        var utcNow = DateTime.UtcNow;
        user.LastLoginAtUtc = utcNow;
        _users.Update(user);
        await _users.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, clientInfo, utcNow, cancellationToken);
    }

    public async Task<AuthResponseModel> RefreshAsync(
        RefreshTokenRequestModel request,
        string? clientInfo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new BusinessRuleException("Refresh token is required.");

        var hash = RefreshTokenCrypto.HashToken(request.RefreshToken);
        var stored = await _refreshTokens.GetActiveByTokenHashAsync(hash, cancellationToken);
        if (stored is null || stored.RevokedAtUtc is not null || stored.ExpiresAtUtc < DateTime.UtcNow)
            throw new BusinessRuleException("Invalid or expired refresh token.");

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new BusinessRuleException("Invalid or expired refresh token.");

        var utcNow = DateTime.UtcNow;
        stored.RevokedAtUtc = utcNow;
        _refreshTokens.Update(stored);

        await _refreshTokens.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, clientInfo, utcNow, cancellationToken);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = RefreshTokenCrypto.HashToken(refreshToken);
        var stored = await _refreshTokens.GetActiveByTokenHashAsync(hash, cancellationToken);
        if (stored is null)
            return;

        stored.RevokedAtUtc = DateTime.UtcNow;
        _refreshTokens.Update(stored);
        await _refreshTokens.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponseModel> IssueTokensAsync(
        User user,
        string? clientInfo,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var accessToken = _jwt.CreateAccessToken(user.Id, user.Email, user.UserName, utcNow, out var accessExpires);

        var plainRefresh = RefreshTokenCrypto.GenerateOpaqueToken();
        var refreshHash = RefreshTokenCrypto.HashToken(plainRefresh);

        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAtUtc = utcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedAtUtc = utcNow,
            ClientInfo = TruncateClientInfo(clientInfo),
        };

        await _refreshTokens.AddAsync(refreshEntity, cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        return new AuthResponseModel
        {
            AccessToken = accessToken,
            RefreshToken = plainRefresh,
            AccessTokenExpiresAtUtc = accessExpires,
            TokenType = "Bearer",
            User = new UserResponseModel
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
            },
        };
    }

    private static string? TruncateClientInfo(string? clientInfo)
    {
        if (string.IsNullOrEmpty(clientInfo))
            return null;
        return clientInfo.Length <= 512 ? clientInfo : clientInfo[..512];
    }
}
