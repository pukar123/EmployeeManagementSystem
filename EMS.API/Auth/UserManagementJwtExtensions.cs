using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Contracts.Roles;
using Pukar.Usermanagement.Contracts.ServiceAuth;

namespace EMS.API.Auth;

/// <summary>
/// Single-host authentication for the consolidated EMS.API deployment.
/// User Management now runs in-process, so EMS validates the JWTs it issues locally
/// using the same signing configuration as <c>IJwtTokenService</c> — there is no longer
/// any remote JWKS fetch. This is the only <c>AddAuthentication</c> registration and it
/// owns the default schemes.
/// </summary>
public static class UserManagementJwtExtensions
{
    public static IServiceCollection AddUserManagementJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtTokenOptions.SectionName);
        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException(
                "Jwt:Issuer and Jwt:Audience are required for the consolidated host to validate User Management tokens.");

        var signingKey = ResolveSigningKey(configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier,
                };
            })
            // Service-token scheme retained as a temporary compatibility surface for the
            // internal UM endpoints. EMS itself no longer acquires or presents service tokens.
            .AddJwtBearer(ServiceAuthConstants.ServiceTokenScheme, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = ServiceAuthConstants.ServiceAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization(auth =>
        {
            auth.AddPolicy("AdminAccess", policy =>
                policy.RequireAssertion(context =>
                    context.User.Claims.Any(c =>
                        c.Type == ClaimTypes.Role
                        && string.Equals(c.Value, WellKnownRoles.Admin, StringComparison.OrdinalIgnoreCase))
                    || context.User.Claims.Any(c =>
                        c.Type == "roles"
                        && string.Equals(c.Value, WellKnownRoles.AdminNormalizedName, StringComparison.OrdinalIgnoreCase))));

            auth.AddPolicy(ServiceScopes.UsersRead, p => RequireScope(p, ServiceScopes.UsersRead));
            auth.AddPolicy(ServiceScopes.UsersManage, p => RequireScope(p, ServiceScopes.UsersManage));
            auth.AddPolicy(ServiceScopes.RolesRead, p => RequireScope(p, ServiceScopes.RolesRead));
            auth.AddPolicy(ServiceScopes.RolesManage, p => RequireScope(p, ServiceScopes.RolesManage));
            auth.AddPolicy(ServiceScopes.InvitationsManage, p => RequireScope(p, ServiceScopes.InvitationsManage));
        });

        return services;
    }

    private static void RequireScope(AuthorizationPolicyBuilder policy, string scope)
    {
        policy.AddAuthenticationSchemes(ServiceAuthConstants.ServiceTokenScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ServiceAuthConstants.ScopeClaimType, scope);
    }

    private static SecurityKey ResolveSigningKey(IConfiguration configuration)
    {
        // Prefer an RSA PEM if one is configured (also allows JWKS publishing for external
        // consumers); otherwise fall back to the shared symmetric signing key.
        var pem = configuration[$"{JwtTokenOptions.SectionName}:SigningKeyPem"];
        var pemFile = configuration[$"{JwtTokenOptions.SectionName}:SigningKeyPemFile"];
        if (string.IsNullOrWhiteSpace(pem) && !string.IsNullOrWhiteSpace(pemFile))
        {
            var path = Path.IsPathRooted(pemFile) ? pemFile : Path.Combine(Directory.GetCurrentDirectory(), pemFile);
            pem = File.ReadAllText(path);
        }

        if (!string.IsNullOrWhiteSpace(pem))
        {
            var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(pem);
            return new RsaSecurityKey(rsa);
        }

        var signingKey = configuration[$"{JwtTokenOptions.SectionName}:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
            throw new InvalidOperationException(
                "Jwt:SigningKeyPem (RSA) or Jwt:SigningKey (32+ chars) is required to validate User Management tokens.");

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }
}
