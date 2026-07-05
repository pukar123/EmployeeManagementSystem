using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Jwt;
using Pukar.Usermanagement.Contracts.Auth;
using Pukar.Usermanagement.Infrastructure.Services;

namespace Pukar.Usermanagement.UnitTests.Infrastructure;

[TestFixture]
public class JwtTokenServiceTests
{
    [Test]
    public void CreateAccessToken_EmitsContractClaimsAndNormalizedRoles_Symmetric()
    {
        var options = Options.Create(new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "12345678901234567890123456789012",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7,
        });
        var service = new JwtTokenService(options);

        var token = service.CreateAccessToken(
            userId: 42,
            email: "admin@example.com",
            userName: "Admin User",
            roleNames: new[] { "admin", " Admin ", "hr_manager" },
            utcNow: DateTime.UtcNow,
            accessTokenExpiresAtUtc: out _);

        AssertClaims(token);
    }

    [Test]
    public void CreateAccessToken_EmitsContractClaimsAndNormalizedRoles_Rsa()
    {
        using var rsa = RSA.Create(2048);
        var pem = rsa.ExportPkcs8PrivateKeyPem();
        var options = Options.Create(new JwtTokenOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKeyPem = pem,
            SigningKeyId = "test-rsa",
            AccessTokenExpirationMinutes = 15,
        });
        IJwksProvider jwks = new RsaJwksProvider(options);
        var service = new JwtTokenService(options, jwks);

        var token = service.CreateAccessToken(
            userId: 42,
            email: "admin@example.com",
            userName: "Admin User",
            roleNames: new[] { "admin", "hr_manager" },
            utcNow: DateTime.UtcNow,
            accessTokenExpiresAtUtc: out _);

        AssertClaims(token);
        Assert.That(jwks.GetJsonWebKeySet().Keys.Count, Is.GreaterThan(0));
    }

    private static void AssertClaims(string token)
    {
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        var claims = jwt.Claims.ToList();

        Assert.That(claims.Any(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub && c.Value == "42"), Is.True);
        Assert.That(claims.Any(c =>
            c.Type == AuthContractConstants.ContractVersionClaimType &&
            c.Value == AuthContractConstants.ContractVersion), Is.True);

        var roleClaims = claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray();
        var rolesCompatClaims = claims.Where(c => c.Type == AuthContractConstants.RolesClaimType).Select(c => c.Value).ToArray();

        Assert.That(roleClaims, Is.EquivalentTo(new[] { "admin", "hr_manager" }));
        Assert.That(rolesCompatClaims, Is.EquivalentTo(new[] { "ADMIN", "HR_MANAGER" }));
    }
}
