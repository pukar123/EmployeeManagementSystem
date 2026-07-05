using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Pukar.Usermanagement.API.Controllers;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Jwt;
using Pukar.Usermanagement.Contracts.ServiceAuth;
using Pukar.Usermanagement.Infrastructure.DependencyInjection;

namespace Pukar.Usermanagement.API.Extensions;

public static class UserManagementApiExtensions
{
    public static IServiceCollection AddPukarUserManagementCore(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "UserManagementDb",
        bool useRsaSigning = true,
        bool registerDbContext = true)
    {
        return services.AddPukarUserManagement(configuration, connectionStringName, useRsaSigning, registerDbContext);
    }

    public static IServiceCollection AddPukarUserManagementAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useRsaSigning = true)
    {
        var jwt = configuration.GetSection(JwtTokenOptions.SectionName);
        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = BuildUserValidationParameters(configuration, useRsaSigning);
            })
            .AddJwtBearer(ServiceAuthConstants.ServiceTokenScheme, options =>
            {
                options.TokenValidationParameters = BuildServiceValidationParameters(configuration, useRsaSigning);
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(ServiceScopes.UsersRead, p => RequireScope(p, ServiceScopes.UsersRead));
            options.AddPolicy(ServiceScopes.UsersManage, p => RequireScope(p, ServiceScopes.UsersManage));
            options.AddPolicy(ServiceScopes.RolesRead, p => RequireScope(p, ServiceScopes.RolesRead));
            options.AddPolicy(ServiceScopes.RolesManage, p => RequireScope(p, ServiceScopes.RolesManage));
            options.AddPolicy(ServiceScopes.InvitationsManage, p => RequireScope(p, ServiceScopes.InvitationsManage));
        });

        return services;
    }

    /// <summary>
    /// Registers user management (EF, auth, refresh tokens) and JWT bearer validation.
    /// </summary>
    public static IServiceCollection AddPukarUserManagementApi(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "UserManagement",
        bool useRsaSigning = false)
    {
        services.AddPukarUserManagementCore(configuration, connectionStringName, useRsaSigning);
        services.AddPukarUserManagementAuthentication(configuration, useRsaSigning);
        return services;
    }

    public static IMvcBuilder AddPukarUserManagementControllers(this IMvcBuilder mvc)
    {
        return mvc.AddApplicationPart(typeof(AuthController).Assembly);
    }

    private static void RequireScope(AuthorizationPolicyBuilder policy, string scope)
    {
        policy.AddAuthenticationSchemes(ServiceAuthConstants.ServiceTokenScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ServiceAuthConstants.ScopeClaimType, scope);
    }

    private static TokenValidationParameters BuildUserValidationParameters(IConfiguration configuration, bool useRsaSigning)
    {
        var jwt = configuration.GetSection(JwtTokenOptions.SectionName);
        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = ResolveSigningKey(configuration, useRsaSigning),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    }

    private static TokenValidationParameters BuildServiceValidationParameters(IConfiguration configuration, bool useRsaSigning)
    {
        var jwt = configuration.GetSection(JwtTokenOptions.SectionName);
        var issuer = jwt["Issuer"];

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = ServiceAuthConstants.ServiceAudience,
            IssuerSigningKey = ResolveSigningKey(configuration, useRsaSigning),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    }

    private static SecurityKey ResolveSigningKey(IConfiguration configuration, bool useRsaSigning)
    {
        if (useRsaSigning)
        {
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
        }

        var signingKey = configuration[$"{JwtTokenOptions.SectionName}:SigningKey"] ?? string.Empty;
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }
}
