using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Pukar.Usermanagement.Contracts.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Jwt;

namespace Pukar.Usermanagement.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IOptions<JwtTokenOptions> _options;
    private readonly IJwksProvider? _jwksProvider;

    public JwtTokenService(IOptions<JwtTokenOptions> options, IJwksProvider? jwksProvider = null)
    {
        _options = options;
        _jwksProvider = jwksProvider;
    }

    public string CreateAccessToken(
        int userId,
        string email,
        string? userName,
        IReadOnlyList<string> roleNames,
        DateTime utcNow,
        out DateTime accessTokenExpiresAtUtc)
    {
        var opt = _options.Value;
        var (credentials, algorithm) = ResolveSigningCredentials(opt);
        var jti = Guid.NewGuid().ToString("N");
        accessTokenExpiresAtUtc = utcNow.AddMinutes(opt.AccessTokenExpirationMinutes);

        var claims = BuildUserClaims(userId, email, userName, roleNames);

        var token = new JwtSecurityToken(
            issuer: opt.Issuer,
            audience: opt.Audience,
            claims: claims,
            notBefore: utcNow,
            expires: accessTokenExpiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateServiceToken(
        string clientId,
        IReadOnlyList<string> scopes,
        DateTime utcNow,
        out DateTime expiresAtUtc)
    {
        var opt = _options.Value;
        var (credentials, _) = ResolveSigningCredentials(opt);
        expiresAtUtc = utcNow.AddMinutes(opt.ServiceTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, clientId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(Contracts.ServiceAuth.ServiceAuthConstants.ClientIdClaimType, clientId),
        };

        foreach (var scope in scopes.Where(s => !string.IsNullOrWhiteSpace(s)))
            claims.Add(new Claim(Contracts.ServiceAuth.ServiceAuthConstants.ScopeClaimType, scope.Trim()));

        var token = new JwtSecurityToken(
            issuer: opt.Issuer,
            audience: Contracts.ServiceAuth.ServiceAuthConstants.ServiceAudience,
            claims: claims,
            notBefore: utcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private (SigningCredentials Credentials, string Algorithm) ResolveSigningCredentials(JwtTokenOptions opt)
    {
        if (_jwksProvider is not null)
        {
            return (new SigningCredentials(_jwksProvider.GetSigningKey(), SecurityAlgorithms.RsaSha256), SecurityAlgorithms.RsaSha256);
        }

        if (string.IsNullOrWhiteSpace(opt.SigningKey) || opt.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKeyPem or Jwt:SigningKey (32+ chars) is required.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opt.SigningKey));
        return (new SigningCredentials(key, SecurityAlgorithms.HmacSha256), SecurityAlgorithms.HmacSha256);
    }

    private static List<Claim> BuildUserClaims(int userId, string email, string? userName, IReadOnlyList<string> roleNames)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(AuthContractConstants.ContractVersionClaimType, AuthContractConstants.ContractVersion),
        };

        if (!string.IsNullOrWhiteSpace(userName))
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, userName));

        var roleNamesTrimmed = roleNames
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Select(static role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var roleName in roleNamesTrimmed)
            claims.Add(new Claim(ClaimTypes.Role, roleName));

        foreach (var roleName in roleNamesTrimmed.Select(static role => role.ToUpperInvariant()))
            claims.Add(new Claim(AuthContractConstants.RolesClaimType, roleName));

        return claims;
    }
}
