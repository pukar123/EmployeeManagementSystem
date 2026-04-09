namespace Pukar.Usermanagement.Application.Services.Jwt;

public interface IJwtTokenService
{
    string CreateAccessToken(int userId, string email, string? userName, DateTime utcNow, out DateTime accessTokenExpiresAtUtc);
}
