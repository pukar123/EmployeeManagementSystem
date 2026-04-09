using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Auth;
using Pukar.Usermanagement.Application.Services.Jwt;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.Repositories.Interface;
using Pukar.Usermanagement.Infrastructure.Repositories;
using Pukar.Usermanagement.Infrastructure.Services;

namespace Pukar.Usermanagement.Infrastructure.DependencyInjection;

public static class UserManagementServiceCollectionExtensions
{
    /// <summary>
    /// Registers user management persistence, auth services, and JWT signing. Call <c>AddPukarUserManagementAuthentication</c> from the API layer to enable bearer validation.
    /// </summary>
    /// <param name="connectionStringName">Connection string name (falls back to <c>DefaultConnection</c>).</param>
    public static IServiceCollection AddPukarUserManagement(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "UserManagement")
    {
        services.Configure<JwtTokenOptions>(configuration.GetSection(JwtTokenOptions.SectionName));

        var connectionString = configuration.GetConnectionString(connectionStringName)
                               ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' or 'DefaultConnection' is required for Pukar user management.");

        services.AddDbContext<UserManagementDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(UserManagementDbContext).Assembly.GetName().Name!)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
