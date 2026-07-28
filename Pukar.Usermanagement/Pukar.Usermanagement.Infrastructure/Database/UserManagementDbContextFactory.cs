using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pukar.Usermanagement.Domain.Database;

namespace Pukar.Usermanagement.Infrastructure.Database;

/// <summary>
/// Design-time factory for EF Core CLI. Prefers the same <c>EMS.API/appsettings.json</c> connection as the host app,
/// then <c>UM_CONNECTION</c>, then LocalDB fallback.
/// </summary>
public sealed class UserManagementDbContextFactory : IDesignTimeDbContextFactory<UserManagementDbContext>
{
    public UserManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserManagementDbContext>();
        var cs = ResolveConnectionString();
        optionsBuilder.UseSqlServer(cs, sql =>
            sql.MigrationsAssembly(typeof(UserManagementDbContext).Assembly.GetName().Name!));
        return new UserManagementDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var fromEnv = Environment.GetEnvironmentVariable("UM_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        var baseDir = Directory.GetCurrentDirectory();
        string? apiRoot = null;
        foreach (var rel in new[]
                 {
                     Path.Combine("Pukar.Usermanagement.Host"),
                     Path.Combine("..", "Pukar.Usermanagement.Host"),
                     Path.Combine("EMS.API"),
                     Path.Combine("..", "EMS.API"),
                     Path.Combine("..", "..", "EMS.API"),
                     Path.Combine("..", "..", "..", "EMS.API"),
                 })
        {
            var candidate = Path.GetFullPath(Path.Combine(baseDir, rel));
            if (Directory.Exists(candidate))
            {
                apiRoot = candidate;
                break;
            }
        }

        if (apiRoot is not null)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiRoot)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var um = configuration.GetConnectionString("UserManagementDb")
                     ?? configuration.GetConnectionString("UserManagement");
            var def = configuration.GetConnectionString("DefaultConnection");
            var resolved = !string.IsNullOrWhiteSpace(um) ? um : def;
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;
        }

        return "Server=(localdb)\\mssqllocaldb;Database=PukarUserManagement;Trusted_Connection=True;TrustServerCertificate=True";
    }
}
