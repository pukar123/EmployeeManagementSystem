namespace Pukar.Usermanagement.Application.Services.Jwt;

public interface IJwtTokenService
{
    string CreateAccessToken(
        int userId,
        string email,
        string? userName,
        IReadOnlyList<string> roleNames,
        DateTime utcNow,
        out DateTime accessTokenExpiresAtUtc);

    string CreateServiceToken(
        string clientId,
        IReadOnlyList<string> scopes,
        DateTime utcNow,
        out DateTime expiresAtUtc);
}
