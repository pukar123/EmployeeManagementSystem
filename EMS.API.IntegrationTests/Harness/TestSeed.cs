using EMS.API.Bootstrap;
using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// The data every access test starts with. Tests refer to these named records, never to raw ids.
/// <list type="bullet">
/// <item>O1 "Test Org" (active).</item>
/// <item>M1, a manager; E1, who reports to M1; E2, who has no manager. All active, all in O1.</item>
/// <item>Menus and ADMIN rights from the real <see cref="EmsRbacSeedHostedService"/>.</item>
/// <item>Role key MANAGER has the "manager.team" menu. Role key EMPLOYEE has no rights at all.</item>
/// </list>
/// </summary>
public static class TestSeed
{
    public const string AdminRoleKey = "ADMIN";
    public const string ManagerRoleKey = "MANAGER";
    public const string EmployeeRoleKey = "EMPLOYEE";
    public const string ManagerTeamMenuKey = "manager.team";

    public static readonly SeededOrganization O1 = new("O1", Id: 1, Name: "Test Org");

    public static readonly SeededEmployee M1 = new("M1", EmployeeId: 101, UserId: 1001, FirstName: "Mia", LastName: "Manager");
    public static readonly SeededEmployee E1 = new("E1", EmployeeId: 102, UserId: 1002, FirstName: "Eli", LastName: "Report");
    public static readonly SeededEmployee E2 = new("E2", EmployeeId: 103, UserId: 1003, FirstName: "Eve", LastName: "Other");

    /// <summary>
    /// Seeds a new, empty database. Fails with a clear message if the production RBAC seed did not
    /// create the ADMIN rights (that seed logs its errors instead of throwing).
    /// </summary>
    internal static async Task SeedAsync(IServiceProvider rootServices, CancellationToken cancellationToken = default)
    {
        var rbacSeed = ActivatorUtilities.CreateInstance<EmsRbacSeedHostedService>(rootServices);
        await rbacSeed.StartAsync(cancellationToken);

        using var scope = rootServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureAdminRightsSeededAsync(db, cancellationToken);

        db.Organizations.Add(new Organization { Id = O1.Id, Name = O1.Name, IsActive = true });
        db.Employees.Add(NewEmployee(M1, managerId: null));
        db.Employees.Add(NewEmployee(E1, managerId: M1.EmployeeId));
        db.Employees.Add(NewEmployee(E2, managerId: null));

        var managerTeamMenuId = await db.Menus.AsNoTracking()
            .Where(m => m.Key == ManagerTeamMenuKey)
            .Select(m => m.Id)
            .SingleAsync(cancellationToken);
        db.RoleKeyPermissions.Add(new RoleKeyPermission
        {
            RoleKey = ManagerRoleKey,
            MenuId = managerTeamMenuId,
            Allowed = true,
        });

        await db.SaveChangesAsync(cancellationToken);

        await EnsureEmployeeRoleHasNoRightsAsync(db, cancellationToken);
    }

    private static Employee NewEmployee(SeededEmployee seeded, int? managerId)
    {
        var now = DateTime.UtcNow;
        return new Employee
        {
            Id = seeded.EmployeeId,
            OrganizationId = O1.Id,
            ManagerId = managerId,
            EmployeeNumber = $"T-{seeded.Label}",
            FirstName = seeded.FirstName,
            LastName = seeded.LastName,
            Email = $"{seeded.Label.ToLowerInvariant()}@ems-tests.invalid",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateJoined = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EmploymentStatus = EmploymentStatus.Active,
            IsActive = true,
            IsArchived = false,
            ExternalIdentityKey = seeded.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    private static async Task EnsureAdminRightsSeededAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var adminMenuCount = await db.RoleKeyPermissions.AsNoTracking()
            .CountAsync(p => p.RoleKey == AdminRoleKey && p.Allowed, cancellationToken);
        var adminCapabilityCount = await db.RoleKeyCapabilities.AsNoTracking()
            .CountAsync(c => c.RoleKey == AdminRoleKey && c.Allowed, cancellationToken);
        var hasManagerTeamMenu = await db.Menus.AsNoTracking()
            .AnyAsync(m => m.Key == ManagerTeamMenuKey, cancellationToken);

        if (adminMenuCount == 0 || adminCapabilityCount == 0 || !hasManagerTeamMenu)
        {
            throw new InvalidOperationException(
                "Test seed failed: EmsRbacSeedHostedService did not create the menus and ADMIN rights " +
                $"(ADMIN menus: {adminMenuCount}, ADMIN capabilities: {adminCapabilityCount}, " +
                $"'{ManagerTeamMenuKey}' menu present: {hasManagerTeamMenu}). See the test log for the seed error.");
        }
    }

    private static async Task EnsureEmployeeRoleHasNoRightsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var hasMenus = await db.RoleKeyPermissions.AsNoTracking()
            .AnyAsync(p => p.RoleKey == EmployeeRoleKey, cancellationToken);
        var hasCapabilities = await db.RoleKeyCapabilities.AsNoTracking()
            .AnyAsync(c => c.RoleKey == EmployeeRoleKey, cancellationToken);

        if (hasMenus || hasCapabilities)
        {
            throw new InvalidOperationException(
                $"Test seed failed: role key {EmployeeRoleKey} must start with no rights " +
                $"(menus: {hasMenus}, capabilities: {hasCapabilities}).");
        }
    }
}
