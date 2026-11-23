using EMS.API.IntegrationTests.Harness;
using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// Extra rows for <see cref="AttendanceAccessTests"/> (B07), added on top of <see cref="TestSeed"/> and never changing
/// O1, M1, E1 or E2. Ids are fixed in the 98xx range, which no other seed uses, so cases can name them.
/// <list type="bullet">
/// <item>Role permissions: <c>ATTENDANCEUSER</c> gets the <c>attendance</c> menu, <c>ATTENDANCEOFFICER</c> only
/// <c>attendance.admin</c>, <c>ATTENDANCEREPORTSONLY</c> <c>attendance.reports</c> and <c>attendance.analytics</c>.
/// Menu ids are looked up by key.</item>
/// <item>O2, a second organisation, with employee Z1 (the other-organisation case).</item>
/// <item>DX, a department in O1, with employee X1 (no manager), who has a record on day D.</item>
/// <item>S1, S2 and S3 in O1 (no manager, no records), one per state-changing self-flow case.</item>
/// <item>Day D (<see cref="DayD"/>, a Monday): one completed record each for M1, E1, E2 and X1.</item>
/// <item>Open sessions: E2 (<see cref="OpenSessionE2"/>, proves a forged check-out stays blocked) and M1
/// (<see cref="OpenSessionM1"/>, closed by an admin in B07-10). Both are outside day D.</item>
/// </list>
/// </summary>
public static class AttendanceAccessSeed
{
    public const string AttendanceUserRole = "AttendanceUser";
    public const string AttendanceOfficerRole = "AttendanceOfficer";
    public const string AttendanceReportsOnlyRole = "AttendanceReportsOnly";

    public const int OrganizationO2 = 9801;
    public const int DepartmentDX = 9801;

    public static readonly SeededEmployee Z1 = new("Z1", EmployeeId: 9811, UserId: 9911, FirstName: "Zoe", LastName: "Elsewhere");
    public static readonly SeededEmployee X1 = new("X1", EmployeeId: 9812, UserId: 9912, FirstName: "Xan", LastName: "Department");
    public static readonly SeededEmployee S1 = new("S1", EmployeeId: 9813, UserId: 9913, FirstName: "Sam", LastName: "SelfOne");
    public static readonly SeededEmployee S2 = new("S2", EmployeeId: 9814, UserId: 9914, FirstName: "Sky", LastName: "SelfTwo");
    public static readonly SeededEmployee S3 = new("S3", EmployeeId: 9815, UserId: 9915, FirstName: "Sid", LastName: "SelfThree");

    /// <summary>Day D: Monday 1 February 2027. Only the seeded completed records fall on it.</summary>
    public static readonly DateTime DayD = new(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    public const int RecordM1OnD = 9801;
    public const int RecordE1OnD = 9802;
    public const int RecordE2OnD = 9803;
    public const int RecordX1OnD = 9804;

    /// <summary>E2's open session, started 2027-02-02 08:00 UTC, no check-out and no breaks.</summary>
    public const int OpenSessionE2 = 9805;

    /// <summary>M1's open session, started 2027-02-03 08:00 UTC, closed by an admin with a supplied time (B07-10).</summary>
    public const int OpenSessionM1 = 9806;

    public static readonly DateTime OpenSessionE2CheckIn = new(2027, 2, 2, 8, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime OpenSessionM1CheckIn = new(2027, 2, 3, 8, 0, 0, DateTimeKind.Utc);

    internal static async Task AddRowsAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var o1 = TestSeed.O1.Id;

        await GrantMenuAsync(db, AttendanceUserRole, "attendance");
        await GrantMenuAsync(db, AttendanceOfficerRole, "attendance.admin");
        await GrantMenuAsync(db, AttendanceReportsOnlyRole, "attendance.reports");
        await GrantMenuAsync(db, AttendanceReportsOnlyRole, "attendance.analytics");

        db.Organizations.Add(new Organization { Id = OrganizationO2, Name = "B07 Other Org", IsActive = true });
        db.Departments.Add(new Department { Id = DepartmentDX, OrganizationId = o1, Name = "B07 Department DX", IsActive = true });

        db.Employees.Add(NewEmployee(Z1, OrganizationO2, departmentId: null, now));
        db.Employees.Add(NewEmployee(X1, o1, DepartmentDX, now));
        db.Employees.Add(NewEmployee(S1, o1, departmentId: null, now));
        db.Employees.Add(NewEmployee(S2, o1, departmentId: null, now));
        db.Employees.Add(NewEmployee(S3, o1, departmentId: null, now));

        db.AttendanceRecords.Add(CompletedOnD(RecordM1OnD, TestSeed.M1.EmployeeId, now));
        db.AttendanceRecords.Add(CompletedOnD(RecordE1OnD, TestSeed.E1.EmployeeId, now));
        db.AttendanceRecords.Add(CompletedOnD(RecordE2OnD, TestSeed.E2.EmployeeId, now));
        db.AttendanceRecords.Add(CompletedOnD(RecordX1OnD, X1.EmployeeId, now));

        db.AttendanceRecords.Add(OpenSession(OpenSessionE2, TestSeed.E2.EmployeeId, OpenSessionE2CheckIn, now));
        db.AttendanceRecords.Add(OpenSession(OpenSessionM1, TestSeed.M1.EmployeeId, OpenSessionM1CheckIn, now));
    }

    private static AttendanceRecord CompletedOnD(int id, int employeeId, DateTime now) => new()
    {
        Id = id,
        OrganizationId = TestSeed.O1.Id,
        EmployeeId = employeeId,
        WorkDate = DayD,
        CheckInAtUtc = DayD.AddHours(9),
        CheckOutAtUtc = DayD.AddHours(17),
        Source = AttendanceSource.Web,
        Status = AttendanceStatus.Completed,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    private static AttendanceRecord OpenSession(int id, int employeeId, DateTime checkInAtUtc, DateTime now) => new()
    {
        Id = id,
        OrganizationId = TestSeed.O1.Id,
        EmployeeId = employeeId,
        WorkDate = checkInAtUtc.Date,
        CheckInAtUtc = checkInAtUtc,
        Source = AttendanceSource.Web,
        Status = AttendanceStatus.Open,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    private static Employee NewEmployee(SeededEmployee seeded, int organizationId, int? departmentId, DateTime now) => new()
    {
        Id = seeded.EmployeeId,
        OrganizationId = organizationId,
        DepartmentId = departmentId,
        ManagerId = null,
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

    private static async Task GrantMenuAsync(AppDbContext db, string roleName, string menuKey)
    {
        var menuId = await db.Menus.AsNoTracking()
            .Where(m => m.Key == menuKey)
            .Select(m => m.Id)
            .SingleAsync();

        db.RoleKeyPermissions.Add(new RoleKeyPermission
        {
            RoleKey = roleName.ToUpperInvariant(),
            MenuId = menuId,
            Allowed = true,
        });
    }
}
