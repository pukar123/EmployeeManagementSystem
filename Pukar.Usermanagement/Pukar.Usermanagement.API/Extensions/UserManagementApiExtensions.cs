using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Pukar.Usermanagement.API.Controllers;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Infrastructure.DependencyInjection;

namespace Pukar.Usermanagement.API.Extensions;

public static class UserManagementApiExtensions
{
    /// <summary>
    /// Registers user management (EF, auth, refresh tokens) and JWT bearer validation. Call after <c>AddControllers()</c> or use <see cref="AddPukarUserManagementControllers"/> on the MVC builder.
    /// </summary>
    public static IServiceCollection AddPukarUserManagementApi(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "UserManagement")
    {
        services.AddPukarUserManagement(configuration, connectionStringName);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = configuration.GetSection(JwtTokenOptions.SectionName);
                var issuer = jwt["Issuer"];
                var audience = jwt["Audience"];
                var signingKey = jwt["SigningKey"];

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? string.Empty)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Registers controllers from this assembly (required when the host references this project as a library).
    /// </summary>
    public static IMvcBuilder AddPukarUserManagementControllers(this IMvcBuilder mvc)
    {
        return mvc.AddApplicationPart(typeof(AuthController).Assembly);
    }
}
