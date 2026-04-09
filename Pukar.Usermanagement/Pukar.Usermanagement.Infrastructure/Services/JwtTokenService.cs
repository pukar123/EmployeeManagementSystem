using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Jwt;

namespace Pukar.Usermanagement.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IOptions<JwtTokenOptions> _options;

    public JwtTokenService(IOptions<JwtTokenOptions> options)
    {
        _options = options;
    }

    public string CreateAccessToken(
        int userId,
        string email,
        string? userName,
        DateTime utcNow,
        out DateTime accessTokenExpiresAtUtc)
    {
        var opt = _options.Value;
        if (string.IsNullOrWhiteSpace(opt.SigningKey) || opt.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jti = Guid.NewGuid().ToString("N");
        accessTokenExpiresAtUtc = utcNow.AddMinutes(opt.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, jti),
        };

        if (!string.IsNullOrWhiteSpace(userName))
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, userName));

        var token = new JwtSecurityToken(
            issuer: opt.Issuer,
            audience: opt.Audience,
            claims: claims,
            notBefore: utcNow,
            expires: accessTokenExpiresAtUtc,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
