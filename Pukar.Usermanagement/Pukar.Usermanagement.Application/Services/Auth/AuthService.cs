using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Pukar.Usermanagement.Contracts.Auth;
using Pukar.Usermanagement.Application.Helpers;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Email;
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
    private readonly IUserRoleRepository _userRoles;
    private readonly IPasswordResetTokenRepository _passwordResetTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicyValidator _passwordPolicy;
    private readonly IEmailSender _emailSender;
    private readonly IJwtTokenService _jwt;
    private readonly JwtTokenOptions _jwtOptions;
    private readonly SmtpOptions _smtpOptions;
    private readonly PasswordResetOptions _passwordResetOptions;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUserRoleRepository userRoles,
        IPasswordResetTokenRepository passwordResetTokens,
        IPasswordHasher passwordHasher,
        IPasswordPolicyValidator passwordPolicy,
        IEmailSender emailSender,
        IJwtTokenService jwt,
        IOptions<JwtTokenOptions> jwtOptions,
        IOptions<SmtpOptions> smtpOptions,
        IOptions<PasswordResetOptions> passwordResetOptions)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _userRoles = userRoles;
        _passwordResetTokens = passwordResetTokens;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
        _emailSender = emailSender;
        _jwt = jwt;
        _jwtOptions = jwtOptions.Value;
        _smtpOptions = smtpOptions.Value;
        _passwordResetOptions = passwordResetOptions.Value;
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
            MustChangePassword = false,
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

    public async Task ChangePasswordAsync(
        int userId,
        ChangePasswordRequestModel request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            throw new BusinessRuleException("Current and new passwords are required.");

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new BusinessRuleException("User was not found.");

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new BusinessRuleException("Current password is incorrect.");

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        _users.Update(user);
        await _users.SaveChangesAsync(cancellationToken);
    }

    public async Task RequestPasswordResetAsync(
        ForgotPasswordRequestModel request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return;

        var normalized = EmailNormalizer.Normalize(request.Email);
        var user = await _users.GetByNormalizedEmailAsync(normalized, cancellationToken);
        if (user is null || !user.IsActive)
            return;

        var utcNow = DateTime.UtcNow;
        var latest = await _passwordResetTokens.GetLatestForUserAsync(user.Id, cancellationToken);
        var cooldown = TimeSpan.FromSeconds(Math.Max(0, _passwordResetOptions.RequestCooldownSeconds));
        if (latest is not null && latest.CreatedAtUtc.Add(cooldown) > utcNow)
            return;

        await _passwordResetTokens.RevokeActiveForUserAsync(user.Id, utcNow, cancellationToken);

        var expirationMinutes = Math.Max(5, _passwordResetOptions.TokenExpirationMinutes);
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashOpaqueToken(rawToken),
            ExpiresAtUtc = utcNow.AddMinutes(expirationMinutes),
            CreatedAtUtc = utcNow,
        };

        await _passwordResetTokens.AddAsync(resetToken, cancellationToken);
        await _passwordResetTokens.SaveChangesAsync(cancellationToken);

        var baseUrl = _smtpOptions.WebAppBaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var greeting = string.IsNullOrWhiteSpace(user.UserName)
            ? "Hello"
            : $"Hello {System.Net.WebUtility.HtmlEncode(user.UserName)}";

        await _emailSender.SendAsync(
            new EmailMessage
            {
                ToEmail = user.Email,
                Subject = "Reset your EMS password",
                Body = $"""
                    <p>{greeting},</p>
                    <p>We received a request to reset your EMS password.</p>
                    <p><a href="{link}">Reset your password</a></p>
                    <p>This link expires in {expirationMinutes} minutes and can only be used once.</p>
                    <p>If you did not request this, you can safely ignore this email.</p>
                    """,
            },
            cancellationToken);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequestModel request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new BusinessRuleException("The password reset link is invalid or expired.");

        var token = await _passwordResetTokens.GetByTokenHashAsync(HashOpaqueToken(request.Token), cancellationToken);
        var utcNow = DateTime.UtcNow;
        if (token is null || token.UsedAtUtc.HasValue || token.ExpiresAtUtc <= utcNow || !token.User.IsActive)
            throw new BusinessRuleException("The password reset link is invalid or expired.");

        _passwordPolicy.ValidateOrThrow(request.NewPassword);

        token.User.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        token.User.MustChangePassword = false;
        token.UsedAtUtc = utcNow;
        _users.Update(token.User);
        _passwordResetTokens.Update(token);
        await _passwordResetTokens.SaveChangesAsync(cancellationToken);

        await _refreshTokens.RevokeAllForUserAsync(token.UserId, utcNow, cancellationToken);
    }

    private static string HashOpaqueToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private async Task<AuthResponseModel> IssueTokensAsync(
        User user,
        string? clientInfo,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var roleNames = await _userRoles.GetRoleNamesForUserAsync(user.Id, cancellationToken);
        var accessToken = _jwt.CreateAccessToken(user.Id, user.Email, user.UserName, roleNames, utcNow, out var accessExpires);

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
            MustChangePassword = user.MustChangePassword,
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
