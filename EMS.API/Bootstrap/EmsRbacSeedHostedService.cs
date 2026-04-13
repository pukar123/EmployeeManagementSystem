using EMS.Domain.Database;
using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.Application;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace EMS.API.Bootstrap;

/// <summary>
/// Seeds EMS <see cref="Menu"/> rows and grants all menus to the system Admin role (um.Roles).
/// Runs after <see cref="AdminUserSeedHostedService"/> so the Admin role exists.
/// </summary>
public sealed class EmsRbacSeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmsRbacSeedHostedService> _logger;

    public EmsRbacSeedHostedService(IServiceProvider serviceProvider, ILogger<EmsRbacSeedHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();

            var adminRole = await roles.GetByNormalizedNameAsync(WellKnownRoles.AdminNormalizedName, cancellationToken);
            if (adminRole is null)
            {
                _logger.LogWarning("EMS RBAC seed skipped: Admin role not found. Apply UserManagement migrations and admin seed first.");
                return;
            }

            var definitions = new[]
            {
                new MenuSeedRow("home", "Home", "/", null, 0, "home"),
                new MenuSeedRow("employees", "Employees", "/employees", null, 10, "users"),
                new MenuSeedRow("departments", "Departments", "/departments", null, 20, "building2"),
                new MenuSeedRow("attendance", "Attendance", "/attendance", null, 30, "clock3"),
                new MenuSeedRow("positions", "Positions", "/positions", null, 40, "briefcase"),
                new MenuSeedRow("sites", "Sites", "/sites", null, 50, "mappin"),
                new MenuSeedRow("organization", "Organization", "/organization/setup", null, 60, "settings"),
                new MenuSeedRow("user-management", "User management", "/user-management", null, 70, "shield"),
                new MenuSeedRow("user-management.users", "Users", "/user-management/users", "user-management", 10, "users"),
                new MenuSeedRow("user-management.roles", "Roles", "/user-management/roles", "user-management", 20, "shield"),
                new MenuSeedRow("user-management.permissions", "Menu access", "/user-management/permissions", "user-management", 30, "layout-list"),
            };

            var keyToId = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var row in definitions)
            {
                var existing = await db.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.Key == row.Key, cancellationToken);
                if (existing is not null)
                {
                    keyToId[row.Key] = existing.Id;
                    continue;
                }

                int? parentId = null;
                if (row.ParentKey is not null)
                {
                    if (!keyToId.TryGetValue(row.ParentKey, out var pid))
                    {
                        var parent = await db.Menus.AsNoTracking().FirstAsync(m => m.Key == row.ParentKey, cancellationToken);
                        pid = parent.Id;
                        keyToId[row.ParentKey] = pid;
                    }

                    parentId = pid;
                }

                var menu = new Menu
                {
                    Key = row.Key,
                    Label = row.Label,
                    RoutePath = row.RoutePath,
                    ParentMenuId = parentId,
                    SortOrder = row.SortOrder,
                    IconKey = row.IconKey,
                };
                db.Menus.Add(menu);
                await db.SaveChangesAsync(cancellationToken);
                keyToId[row.Key] = menu.Id;
            }

            var menuIds = await db.Menus.AsNoTracking().Select(m => m.Id).ToListAsync(cancellationToken);
            foreach (var menuId in menuIds)
            {
                var has = await db.RolePermissions.AsNoTracking()
                    .AnyAsync(rp => rp.RoleId == adminRole.Id && rp.MenuId == menuId, cancellationToken);
                if (has)
                    continue;

                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    MenuId = menuId,
                    Allowed = true,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("EMS RBAC seed completed for Admin role id {RoleId}.", adminRole.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EMS RBAC seed failed. Ensure EMS migrations are applied.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private sealed record MenuSeedRow(
        string Key,
        string Label,
        string RoutePath,
        string? ParentKey,
        int SortOrder,
        string? IconKey);
}
