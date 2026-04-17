using EMS.Domain.Database;
using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.Bootstrap;

/// <summary>
/// Seeds EMS <see cref="Menu"/> rows and grants all menus to the ADMIN role key.
/// </summary>
public sealed class EmsRbacSeedHostedService : IHostedService
{
    private const string AdminRoleKey = "ADMIN";

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

            var definitions = new[]
            {
                new MenuSeedRow("home", "Home", "/", null, 0, "home"),
                new MenuSeedRow("employees", "Employees", "/employees", null, 10, "users"),
                new MenuSeedRow("departments", "Departments", "/departments", null, 20, "building2"),
                new MenuSeedRow("attendance", "Attendance", "/attendance", null, 30, "clock3"),
                new MenuSeedRow("tasks", "Tasks", "/tasks", null, 35, "list-todo"),
                new MenuSeedRow("positions", "Positions", "/positions", null, 40, "briefcase"),
                new MenuSeedRow("sites", "Sites", "/sites", null, 50, "mappin"),
                new MenuSeedRow("organization", "Organization", "/organization/setup", null, 60, "settings"),
                new MenuSeedRow("user-management", "User management", "/user-management", null, 70, "shield"),
                new MenuSeedRow("user-management.users", "Users", "/user-management/users", "user-management", 10, "users"),
                new MenuSeedRow("user-management.roles", "Roles", "/user-management/roles", "user-management", 20, "shield"),
                new MenuSeedRow("user-management.menu-access", "Menu access", "/user-management/menu-access", "user-management", 30, "layout-list"),
            };

            var keyToId = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var row in definitions)
            {
                int? parentId = null;
                if (row.ParentKey is not null)
                {
                    if (!keyToId.TryGetValue(row.ParentKey, out var pid))
                    {
                        var parent = await db.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.Key == row.ParentKey, cancellationToken);
                        if (parent is not null)
                        {
                            pid = parent.Id;
                            keyToId[row.ParentKey] = pid;
                        }
                    }

                    if (keyToId.TryGetValue(row.ParentKey, out var resolvedParentId))
                        parentId = resolvedParentId;
                }

                var existing = await db.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.Key == row.Key, cancellationToken);
                if (existing is not null)
                {
                    keyToId[row.Key] = existing.Id;
                    continue;
                }

                var route = row.RoutePath.Trim();
                if (route.Length > 1)
                {
                    route = route.TrimEnd('/');
                }

                var existingByRouteAndParent = await db.Menus.AsNoTracking().FirstOrDefaultAsync(
                    m => m.ParentMenuId == parentId &&
                         (m.RoutePath == route || m.RoutePath == route + "/"),
                    cancellationToken);
                if (existingByRouteAndParent is not null)
                {
                    keyToId[row.Key] = existingByRouteAndParent.Id;
                    continue;
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
                var has = await db.RoleKeyPermissions.AsNoTracking()
                    .AnyAsync(rp => rp.RoleKey == AdminRoleKey && rp.MenuId == menuId, cancellationToken);
                if (has)
                    continue;

                db.RoleKeyPermissions.Add(new RoleKeyPermission
                {
                    RoleKey = AdminRoleKey,
                    MenuId = menuId,
                    Allowed = true,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("EMS RBAC seed completed for role key {RoleKey}.", AdminRoleKey);
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
