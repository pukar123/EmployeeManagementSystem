using System.Net;
using EMS.API.IntegrationTests.Harness;
using EMS.Application.DTOs.Department;
using EMS.Application.DTOs.JobPosition;
using EMS.Application.DTOs.Leave;
using EMS.Application.DTOs.Location;
using EMS.Application.DTOs.Navigation;
using EMS.Application.DTOs.Organization;
using EMS.Application.DTOs.Site;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// B04: role assignment and the setup screens' create, change and delete actions are admin-only,
/// while the reads those screens and the everyday forms use stay open to any signed-in user.
/// This fixture has its own database (with <see cref="AdminSetupSeed"/> rows), so its write cases
/// never touch the shared seed used by <see cref="WhoCanDoWhatTests"/>.
/// Denied requests carry no body: authorisation runs before model binding, so they are refused first.
/// </summary>
[TestFixture]
public sealed class AdminSetupAccessTests : AccessTestFixture
{
    private static readonly int O1 = TestSeed.O1.Id;

    private static readonly string RolePositionRoles = $"/api/JobPositions/{AdminSetupSeed.RolePositionId}/roles";
    private static readonly string Organizations = "/api/Organizations";
    private static readonly string Organization = $"/api/Organizations/{AdminSetupSeed.OrganizationId}";
    private static readonly string OrganizationLogo = $"/api/Organizations/{AdminSetupSeed.OrganizationId}/logo";
    private static readonly string Departments = "/api/Departments";
    private static readonly string Department = $"/api/Departments/{AdminSetupSeed.DepartmentId}";
    private static readonly string Sites = "/api/Sites";
    private static readonly string Site = $"/api/Sites/{AdminSetupSeed.SiteId}";
    private static readonly string SiteEmployees = $"/api/Sites/{AdminSetupSeed.SiteId}/employees";
    private static readonly string SiteEmployeeE1 = $"/api/Sites/{AdminSetupSeed.SiteId}/employees/{TestSeed.E1.EmployeeId}";
    private static readonly string Locations = "/api/Locations";
    private static readonly string Location = $"/api/Locations/{AdminSetupSeed.LocationId}";
    private static readonly string JobPositions = "/api/JobPositions";
    private static readonly string JobPositionsInO1 = $"/api/JobPositions?organizationId={O1}";
    private static readonly string JobPosition = $"/api/JobPositions/{AdminSetupSeed.JobPositionId}";
    private static readonly string Menus = "/api/Menus";
    private static readonly string Menu = $"/api/Menus/{AdminSetupSeed.MenuId}";
    private static readonly string LeaveTypes = "/api/LeaveTypes";
    private static readonly string LeaveTypesInO1 = $"/api/LeaveTypes?organizationId={O1}";
    private static readonly string LeaveType = $"/api/LeaveTypes/{AdminSetupSeed.LeaveTypeId}";
    private static readonly string LeavePolicyRules = "/api/LeavePolicyRules";
    private static readonly string LeavePolicyRulesForLeaveType = $"/api/LeavePolicyRules/leave-type/{AdminSetupSeed.LeaveTypeId}";
    private static readonly string LeavePolicyRule = $"/api/LeavePolicyRules/{AdminSetupSeed.LeavePolicyRuleId}";
    private static readonly string LeaveAccrualRun = "/api/LeaveOperations/accrual/run";
    private static readonly string LeaveYearResetRun = "/api/LeaveOperations/year-reset/run";
    private static readonly string LeaveBulkImport = "/api/LeaveImports/bulk";

    // A role key the test User Management data accepts. Its role list is empty, so the service skips
    // role-name validation, but MANAGER would also be valid against the real role list.
    private const string AllowedRoleKey = TestSeed.ManagerRoleKey;

    [OneTimeSetUp]
    public async Task AddAdminSetupRowsAsync()
    {
        await Factory.ChangeDatabaseAsync(AdminSetupSeed.AddRowsAsync);
    }

    private static IEnumerable<TestCaseData> StatusCases()
    {
        var employee = TestUsers.Employee(TestSeed.E1);
        var manager = TestUsers.Manager(TestSeed.M1);
        var admin = TestUsers.Admin;

        // Job position roles: reads stay open.
        yield return Case("B04-04", employee, HttpMethod.Get, RolePositionRoles, HttpStatusCode.OK);

        // Organisations.
        yield return Case("B04-05", employee, HttpMethod.Post, Organizations, HttpStatusCode.Forbidden);
        yield return Case("B04-06", employee, HttpMethod.Put, Organization, HttpStatusCode.Forbidden);
        yield return Case("B04-07", employee, HttpMethod.Post, OrganizationLogo, HttpStatusCode.Forbidden);
        yield return Case("B04-11", TestUsers.NoRole, HttpMethod.Get, Organizations, HttpStatusCode.OK);

        // Departments.
        yield return Case("B04-12", employee, HttpMethod.Post, Departments, HttpStatusCode.Forbidden);
        yield return Case("B04-13", employee, HttpMethod.Put, Department, HttpStatusCode.Forbidden);
        yield return Case("B04-13", employee, HttpMethod.Delete, Department, HttpStatusCode.Forbidden);
        yield return Case("B04-14", admin, HttpMethod.Post, Departments, HttpStatusCode.Created,
            new DepartmentDTO { OrganizationId = O1, Name = "B04 Dept", IsActive = true });
        yield return Case("B04-15", employee, HttpMethod.Get, Departments, HttpStatusCode.OK);

        // Sites, including linking and unlinking employees.
        yield return Case("B04-16", employee, HttpMethod.Post, Sites, HttpStatusCode.Forbidden);
        yield return Case("B04-16", employee, HttpMethod.Put, Site, HttpStatusCode.Forbidden);
        yield return Case("B04-16", employee, HttpMethod.Delete, Site, HttpStatusCode.Forbidden);
        yield return Case("B04-17", employee, HttpMethod.Post, SiteEmployees, HttpStatusCode.Forbidden);
        yield return Case("B04-17", employee, HttpMethod.Delete, SiteEmployeeE1, HttpStatusCode.Forbidden);
        yield return Case("B04-18", admin, HttpMethod.Post, Sites, HttpStatusCode.Created,
            new CreateSiteRequestModel { SiteName = "B04 Site", SiteLocation = "West Wyalong", IsActive = true });
        yield return Case("B04-19", employee, HttpMethod.Get, Sites, HttpStatusCode.OK);

        // Locations.
        yield return Case("B04-20", employee, HttpMethod.Post, Locations, HttpStatusCode.Forbidden);
        yield return Case("B04-20", employee, HttpMethod.Put, Location, HttpStatusCode.Forbidden);
        yield return Case("B04-20", employee, HttpMethod.Delete, Location, HttpStatusCode.Forbidden);
        yield return Case("B04-21", admin, HttpMethod.Post, Locations, HttpStatusCode.Created,
            new CreateLocationRequestModel { OrganizationId = O1, Name = "B04 Location", IsActive = true });
        yield return Case("B04-22", employee, HttpMethod.Get, Locations, HttpStatusCode.OK);

        // Job positions.
        yield return Case("B04-23", employee, HttpMethod.Post, JobPositions, HttpStatusCode.Forbidden);
        yield return Case("B04-23", employee, HttpMethod.Put, JobPosition, HttpStatusCode.Forbidden);
        yield return Case("B04-23", employee, HttpMethod.Delete, JobPosition, HttpStatusCode.Forbidden);
        yield return Case("B04-24", admin, HttpMethod.Post, JobPositions, HttpStatusCode.Created,
            new CreateJobPositionRequestModel { OrganizationId = O1, Title = "B04 Position", IsActive = true });
        yield return Case("B04-25", employee, HttpMethod.Get, JobPositionsInO1, HttpStatusCode.OK);

        // Menus.
        yield return Case("B04-26", employee, HttpMethod.Post, Menus, HttpStatusCode.Forbidden);
        yield return Case("B04-26", employee, HttpMethod.Put, Menu, HttpStatusCode.Forbidden);
        yield return Case("B04-26", employee, HttpMethod.Delete, Menu, HttpStatusCode.Forbidden);
        yield return Case("B04-27", admin, HttpMethod.Post, Menus, HttpStatusCode.Created,
            new CreateMenuRequestModel { Key = "b04.new", Label = "B04 New", RoutePath = "/b04-new", SortOrder = 999 });
        yield return Case("B04-28", employee, HttpMethod.Get, Menus, HttpStatusCode.OK);

        // Leave types.
        yield return Case("B04-29", employee, HttpMethod.Post, LeaveTypes, HttpStatusCode.Forbidden);
        yield return Case("B04-29", employee, HttpMethod.Put, LeaveType, HttpStatusCode.Forbidden);
        yield return Case("B04-29", employee, HttpMethod.Delete, LeaveType, HttpStatusCode.Forbidden);
        yield return Case("B04-30", admin, HttpMethod.Post, LeaveTypes, HttpStatusCode.Created,
            new CreateLeaveTypeRequestModel { OrganizationId = O1, Name = "B04 Leave", IsActive = true });
        yield return Case("B04-31", employee, HttpMethod.Get, LeaveTypesInO1, HttpStatusCode.OK);

        // Leave policy rules.
        yield return Case("B04-32", employee, HttpMethod.Post, LeavePolicyRules, HttpStatusCode.Forbidden);
        yield return Case("B04-32", employee, HttpMethod.Put, LeavePolicyRule, HttpStatusCode.Forbidden);
        yield return Case("B04-32", employee, HttpMethod.Delete, LeavePolicyRule, HttpStatusCode.Forbidden);
        yield return Case("B04-33", admin, HttpMethod.Post, LeavePolicyRules, HttpStatusCode.Created,
            new CreateLeavePolicyRuleRequestModel
            {
                OrganizationId = O1,
                LeaveTypeId = AdminSetupSeed.LeaveTypeId,
                AccrualRatePerPeriod = 1.5m,
                AccrualFrequency = AccrualFrequency.Monthly,
                EffectiveFromDateUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
            });
        yield return Case("B04-34", employee, HttpMethod.Get, LeavePolicyRulesForLeaveType, HttpStatusCode.OK);

        // Leave accrual and year-reset runs.
        yield return Case("B04-35", employee, HttpMethod.Post, LeaveAccrualRun, HttpStatusCode.Forbidden);
        yield return Case("B04-36", manager, HttpMethod.Post, LeaveYearResetRun, HttpStatusCode.Forbidden);
        // Run in O-B04-Leave, not O1: see AdminSetupSeed.AccrualOrganizationId for the InMemory reason.
        yield return Case("B04-37", admin, HttpMethod.Post, LeaveAccrualRun, HttpStatusCode.OK,
            new RunLeaveAccrualRequestModel { OrganizationId = AdminSetupSeed.AccrualOrganizationId, AsOfUtc = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc) });
        yield return Case("B04-38", admin, HttpMethod.Post, LeaveYearResetRun, HttpStatusCode.OK,
            new RunLeaveYearResetRequestModel { OrganizationId = O1, LeaveYearStartDateUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) });

        // Bulk leave import.
        yield return Case("B04-39", employee, HttpMethod.Post, LeaveBulkImport, HttpStatusCode.Forbidden);
        yield return Case("B04-40", admin, HttpMethod.Post, LeaveBulkImport, HttpStatusCode.OK,
            new BulkLeaveImportRequestModel { OrganizationId = O1, Items = [] });

        // Signed-out callers are still refused before any policy runs.
        yield return Case("B04-41", TestUsers.Anonymous, HttpMethod.Put, RolePositionRoles, HttpStatusCode.Unauthorized);
        yield return Case("B04-41", TestUsers.Anonymous, HttpMethod.Post, LeaveAccrualRun, HttpStatusCode.Unauthorized);
    }

    [TestCaseSource(nameof(StatusCases))]
    public async Task Request_ReturnsExpectedStatus(
        TestUser user, HttpMethod method, string path, HttpStatusCode expected, object? body)
    {
        await ExpectStatusAsync(user, method, path, expected, body);
    }

    // B04-01 to B04-03 run first and in this order: the denied cases check that P-B04 still has no roles
    // and that no sync was started, and B04-03 then shows the recorder does see a real sync.

    [Test, Order(1)]
    public async Task B04_01_EmployeeAddsAdminToPosition_IsRefusedAndNothingChanges()
    {
        await ExpectRoleChangeRefusedAsync(TestUsers.Employee(TestSeed.E1));
    }

    [Test, Order(2)]
    public async Task B04_02_ManagerAddsAdminToPosition_IsRefusedAndNothingChanges()
    {
        await ExpectRoleChangeRefusedAsync(TestUsers.Manager(TestSeed.M1));
    }

    [Test, Order(3)]
    public async Task B04_03_AdminSetsPositionRoles_SavesRolesAndSyncsEmployees()
    {
        var request = new SetPositionRolesRequestModel { RoleKeys = [AllowedRoleKey] };

        await ExpectStatusAsync(TestUsers.Admin, HttpMethod.Put, RolePositionRoles, HttpStatusCode.NoContent, request);

        var storedRoleKeys = await StoredRoleKeysAsync();
        var positionSyncs = Factory.RoleSyncs.Calls
            .Where(c => c.Method == RoleSyncRecorder.SyncEmployeesForPositionMethod && c.Id == AdminSetupSeed.RolePositionId)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(storedRoleKeys, Is.EquivalentTo(new[] { AllowedRoleKey }),
                $"Admin -> PUT {RolePositionRoles}: expected P-B04 to hold only {AllowedRoleKey}.");
            Assert.That(positionSyncs, Has.Count.EqualTo(1),
                $"Admin -> PUT {RolePositionRoles}: expected one role sync for P-B04. Recorded: {DescribeSyncs()}");
        });
    }

    [Test]
    public async Task B04_08_EmployeeDeletesOrganization_IsRefusedAndOrganizationRemains()
    {
        await ExpectStatusAsync(TestUsers.Employee(TestSeed.E1), HttpMethod.Delete, Organization, HttpStatusCode.Forbidden);

        Assert.That(
            await OrganizationExistsAsync(AdminSetupSeed.OrganizationId),
            Is.True,
            $"Employee E1 -> DELETE {Organization}: O-B04 should still exist after a refused delete.");
    }

    /// <summary>
    /// The admin gets past the new policy and reaches the service. EMS allows one organisation per
    /// instance and this database already has O1, so the service's existing answer is 409 (not 201).
    /// First-run setup (no organisation yet) cannot be shown here without removing O1 from the shared seed.
    /// </summary>
    [Test]
    public async Task B04_09_AdminCreatesOrganization_PassesPolicyAndReachesOneOrganizationRule()
    {
        var user = TestUsers.Admin;
        var body = await ExpectStatusAsync(user, HttpMethod.Post, Organizations, HttpStatusCode.Conflict,
            new OrganizationDTO { Name = "B04 Org New", IsActive = true });

        Assert.That(body, Does.Contain("Only one organization"),
            BodyMessage(user, HttpMethod.Post, Organizations, "the one-organisation message", "in the body", body));
    }

    [Test]
    public async Task B04_10_AdminDeletesOrganization_RemovesIt()
    {
        var path = $"/api/Organizations/{AdminSetupSeed.OrganizationToDeleteId}";

        await ExpectStatusAsync(TestUsers.Admin, HttpMethod.Delete, path, HttpStatusCode.NoContent);

        Assert.That(
            await OrganizationExistsAsync(AdminSetupSeed.OrganizationToDeleteId),
            Is.False,
            $"Admin -> DELETE {path}: O-B04-Delete should be gone.");
    }

    private async Task ExpectRoleChangeRefusedAsync(TestUser user)
    {
        // Denied requests carry a real body here, to show that even a well-formed ADMIN request is refused.
        var request = new SetPositionRolesRequestModel { RoleKeys = [TestSeed.AdminRoleKey] };

        await ExpectStatusAsync(user, HttpMethod.Put, RolePositionRoles, HttpStatusCode.Forbidden, request);

        var storedRoleKeys = await StoredRoleKeysAsync();
        var syncsForPositionOrEmployee = Factory.RoleSyncs.Calls
            .Where(c =>
                (c.Method == RoleSyncRecorder.SyncEmployeesForPositionMethod && c.Id == AdminSetupSeed.RolePositionId)
                || (c.Method == RoleSyncRecorder.SyncEmployeeMethod && c.Id == AdminSetupSeed.RolePositionEmployeeId))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(storedRoleKeys, Is.Empty,
                $"{user.Description} -> PUT {RolePositionRoles}: P-B04 should still have no roles, " +
                $"found: {string.Join(", ", storedRoleKeys)}.");
            Assert.That(syncsForPositionOrEmployee, Is.Empty,
                $"{user.Description} -> PUT {RolePositionRoles}: no role sync should start. Recorded: {DescribeSyncs()}");
        });
    }

    private Task<List<string>> StoredRoleKeysAsync()
        => Factory.QueryDatabaseAsync(db => db.PositionRoles.AsNoTracking()
            .Where(r => r.JobPositionId == AdminSetupSeed.RolePositionId)
            .Select(r => r.RoleKey)
            .ToListAsync());

    private Task<bool> OrganizationExistsAsync(int organizationId)
        => Factory.QueryDatabaseAsync(db => db.Organizations.AsNoTracking().AnyAsync(o => o.Id == organizationId));

    private string DescribeSyncs()
    {
        var calls = Factory.RoleSyncs.Calls;
        return calls.Count == 0 ? "(none)" : string.Join(", ", calls.Select(c => $"{c.Method}({c.Id})"));
    }
}
