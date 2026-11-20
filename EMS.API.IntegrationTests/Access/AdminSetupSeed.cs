using EMS.API.IntegrationTests.Harness;
using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// Extra rows for <see cref="AdminSetupAccessTests"/>, added on top of <see cref="TestSeed"/> and never
/// touching O1, M1, E1 or E2. Ids are fixed (in a range the starting seed does not use) so that test
/// rows can name them; rows the API creates during a test get their own generated ids.
/// <list type="bullet">
/// <item>P-B04, a job position in O1 with no position roles, held by employee E-B04 (no linked user).</item>
/// <item>One department, site, location, job position, menu (key <c>b04.test</c>), leave type and leave policy rule
/// for the change and delete cases.</item>
/// <item>O-B04, a second organisation for the denied update, logo and delete cases, and O-B04-Delete for the
/// admin delete case (so no case depends on another having run first).</item>
/// </list>
/// </summary>
public static class AdminSetupSeed
{
    public const int RolePositionId = 9001;          // P-B04
    public const int RolePositionEmployeeId = 9001;  // E-B04
    public const int OrganizationId = 9001;          // O-B04
    public const int OrganizationToDeleteId = 9002;  // O-B04-Delete
    public const int DepartmentId = 9001;
    public const int SiteId = 9001;
    public const int LocationId = 9001;
    public const int JobPositionId = 9002;
    public const int MenuId = 9001;
    public const string MenuKey = "b04.test";
    public const int LeaveTypeId = 9001;
    public const int LeavePolicyRuleId = 9001;

    internal static Task AddRowsAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var o1 = TestSeed.O1.Id;

        db.Organizations.Add(new Organization { Id = OrganizationId, Name = "B04 Org", IsActive = true });
        db.Organizations.Add(new Organization { Id = OrganizationToDeleteId, Name = "B04 Org To Delete", IsActive = true });

        db.JobPositions.Add(new JobPosition { Id = RolePositionId, OrganizationId = o1, Title = "B04 Role Position", IsActive = true });
        db.JobPositions.Add(new JobPosition { Id = JobPositionId, OrganizationId = o1, Title = "B04 Setup Position", IsActive = true });
        db.Employees.Add(new Employee
        {
            Id = RolePositionEmployeeId,
            OrganizationId = o1,
            JobPositionId = RolePositionId,
            EmployeeNumber = "T-E-B04",
            FirstName = "Ben",
            LastName = "Position",
            Email = "e-b04@ems-tests.invalid",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateJoined = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EmploymentStatus = EmploymentStatus.Active,
            IsActive = true,
            IsArchived = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        db.Departments.Add(new Department { Id = DepartmentId, OrganizationId = o1, Name = "B04 Setup Department", IsActive = true });
        db.Sites.Add(new Site { SiteId = SiteId, SiteName = "B04 Setup Site", SiteLocation = "West Wyalong", IsActive = true });
        db.Locations.Add(new Location { Id = LocationId, OrganizationId = o1, Name = "B04 Setup Location", IsActive = true });
        db.Menus.Add(new Menu { Id = MenuId, Key = MenuKey, Label = "B04 Test", RoutePath = "/b04-test", SortOrder = 998 });

        db.LeaveTypes.Add(new LeaveType
        {
            Id = LeaveTypeId,
            OrganizationId = o1,
            Name = "B04 Setup Leave",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            Id = LeavePolicyRuleId,
            OrganizationId = o1,
            LeaveTypeId = LeaveTypeId,
            AccrualRatePerPeriod = 1m,
            AccrualFrequency = AccrualFrequency.Monthly,
            IsActive = true,
            EffectiveFromDateUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        return Task.CompletedTask;
    }
}
