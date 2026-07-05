using System.Security.Claims;
using EMS.Application.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Pukar.Usermanagement.Contracts.Roles;

namespace EMS.API.Auth;

public static class UserManagementJwtExtensions
{
    public static IServiceCollection AddUserManagementJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(UserManagementApiOptions.SectionName).Get<UserManagementApiOptions>()
            ?? new UserManagementApiOptions();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException("UserManagementApi:BaseUrl is required for JWT validation.");

        var jwksUri = new Uri(new Uri(options.BaseUrl.TrimEnd('/') + "/"), options.JwksPath.TrimStart('/'));
        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            jwksUri.ToString(),
            new JwksConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = jwksUri.Scheme == Uri.UriSchemeHttps })
        {
            AutomaticRefreshInterval = TimeSpan.FromMinutes(30),
            RefreshInterval = TimeSpan.FromMinutes(5),
        };

        services.AddSingleton(configurationManager);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwt =>
            {
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = options.JwtAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    IssuerSigningKeyResolver = (_, _, _, _) =>
                    {
                        var config = configurationManager.GetConfigurationAsync(CancellationToken.None)
                            .GetAwaiter()
                            .GetResult();
                        return config.SigningKeys;
                    },
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
        });

        return services;
    }

    private sealed class JwksConfigurationRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
    {
        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
            string address,
            IDocumentRetriever retriever,
            CancellationToken cancel)
        {
            var document = await retriever.GetDocumentAsync(address, cancel);
            var keys = new JsonWebKeySet(document);
            var config = new OpenIdConnectConfiguration();
            foreach (var key in keys.GetSigningKeys())
                config.SigningKeys.Add(key);
            return config;
        }
    }
}
