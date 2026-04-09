using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pukar.Usermanagement.Domain.Database;

namespace Pukar.Usermanagement.Infrastructure.Database;

/// <summary>Design-time factory for EF Core CLI (migrations). Override connection string via environment or appsettings in the host project.</summary>
public sealed class UserManagementDbContextFactory : IDesignTimeDbContextFactory<UserManagementDbContext>
{
    public UserManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserManagementDbContext>();
        var cs = Environment.GetEnvironmentVariable("UM_CONNECTION")
                   ?? "Server=(localdb)\\mssqllocaldb;Database=PukarUserManagement;Trusted_Connection=True;TrustServerCertificate=True";
        optionsBuilder.UseSqlServer(cs, sql =>
            sql.MigrationsAssembly(typeof(UserManagementDbContext).Assembly.GetName().Name!));
        return new UserManagementDbContext(optionsBuilder.Options);
    }
}
