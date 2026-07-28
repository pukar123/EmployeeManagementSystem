using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Auth;
using Pukar.Usermanagement.Application.Services.Email;
using Pukar.Usermanagement.Application.Services.Internal;
using Pukar.Usermanagement.Application.Services.Invitations;
using Pukar.Usermanagement.Application.Services.Jwt;
using Pukar.Usermanagement.Application.Services.Roles;
using Pukar.Usermanagement.Application.Services.ServiceAuth;
using Pukar.Usermanagement.Application.Services.UserRoles;
using Pukar.Usermanagement.Application.Services.Users;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.Repositories.Interface;
using Pukar.Usermanagement.Infrastructure.Email;
using Pukar.Usermanagement.Infrastructure.Repositories;
using Pukar.Usermanagement.Infrastructure.Services;

namespace Pukar.Usermanagement.Infrastructure.DependencyInjection;

public static class UserManagementServiceCollectionExtensions
{
    public static IServiceCollection AddPukarUserManagement(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "UserManagementDb",
        bool useRsaSigning = true,
        bool registerDbContext = true)
    {
        services.Configure<JwtTokenOptions>(configuration.GetSection(JwtTokenOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<ServiceClientsOptions>(configuration.GetSection(ServiceClientsOptions.SectionName));
        services.Configure<PasswordPolicyOptions>(configuration.GetSection(PasswordPolicyOptions.SectionName));
        services.Configure<PasswordResetOptions>(configuration.GetSection(PasswordResetOptions.SectionName));

        if (registerDbContext)
        {
            // The User Management bounded context owns its own database. It must never
            // silently fall back to the EMS DefaultConnection, which would merge the two
            // databases. Fail fast with a clear message when the connection string is missing.
            var connectionString = configuration.GetConnectionString(connectionStringName)
                                   ?? configuration.GetConnectionString("UserManagement");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Connection string 'ConnectionStrings:{connectionStringName}' is required for the User Management " +
                    "bounded context. Set it in configuration, User Secrets, or environment variables. " +
                    "It must point at UserManagementDb and must not be shared with the EMS DefaultConnection.");

            services.AddDbContext<UserManagementDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(UserManagementDbContext).Assembly.GetName().Name!)));
        }

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAccountInvitationRepository, AccountInvitationRepository>();
        services.AddScoped<IIdempotencyRecordRepository, IdempotencyRecordRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IServiceClientRepository, ServiceClientRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IPasswordPolicyValidator, PasswordPolicyValidator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        if (useRsaSigning)
        {
            services.AddSingleton<IJwksProvider, RsaJwksProvider>();
        }

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IInternalUserService, InternalUserService>();
        services.AddScoped<IInternalRoleService, InternalRoleService>();
        services.AddScoped<IServiceAuthService, ServiceAuthService>();

        return services;
    }
}
