using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Pukar.Usermanagement.Application.Auth;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Infrastructure.Services;

namespace Pukar.Usermanagement.UnitTests.Infrastructure;

[TestFixture]
public class JwtTokenServiceTests
{
    [Test]
    public void CreateAccessToken_EmitsContractClaimsAndNormalizedRoles()
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

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var claims = jwt.Claims.ToList();

        Assert.That(claims.Any(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "42"), Is.True);
        Assert.That(claims.Any(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "admin@example.com"), Is.True);
        Assert.That(claims.Any(c =>
            c.Type == AuthContractConstants.ContractVersionClaimType &&
            c.Value == AuthContractConstants.ContractVersion), Is.True);

        var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        var rolesCompatClaims = claims.Where(c => c.Type == AuthContractConstants.RolesClaimType).Select(c => c.Value).ToArray();

        Assert.That(roleClaims, Is.EquivalentTo(new[] { "admin", "hr_manager" }));
        Assert.That(rolesCompatClaims, Is.EquivalentTo(new[] { "ADMIN", "HR_MANAGER" }));
    }
}
