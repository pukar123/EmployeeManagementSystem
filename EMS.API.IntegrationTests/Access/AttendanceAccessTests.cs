using System.Globalization;
using System.Net;
using System.Text.Json;
using EMS.API.IntegrationTests.Harness;
using EMS.Application.DTOs.Attendance;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// B07: attendance is safe. Employees record only their own time (with server times), manual entries and acting for
/// someone else need an attendance admin (an admin, or a role granted the <c>attendance.admin</c> menu), per-employee
/// reads are for self, attendance admins and the current direct manager, and reports, analytics and exports are
/// scoped on the server: self for employees, self plus direct reports for managers, everyone for attendance admins.
/// The <c>attendance</c>, <c>attendance.reports</c> and <c>attendance.analytics</c> menus grant nothing over others.
/// This fixture has its own database with <see cref="AttendanceAccessSeed"/> rows. Every state-changing case uses its
/// own employee or session, so the order of tests does not matter.
/// </summary>
[TestFixture]
public sealed class AttendanceAccessTests : AccessTestFixture
{
    private static readonly int O1 = TestSeed.O1.Id;
    private static readonly int M1 = TestSeed.M1.EmployeeId;
    private static readonly int E1 = TestSeed.E1.EmployeeId;
    private static readonly int E2 = TestSeed.E2.EmployeeId;
    private static readonly int X1 = AttendanceAccessSeed.X1.EmployeeId;
    private const int MissingEmployeeId = 999999;

    private const string CheckIn = "/api/Attendance/check-in";
    private const string CheckOut = "/api/Attendance/check-out";
    private const string BreakStart = "/api/Attendance/break/start";
    private const string BreakEnd = "/api/Attendance/break/end";
    private const string ManualEntry = "/api/Attendance/manual-entry";
    private const string Eligibility = "/api/EmployeePortal/eligibility";

    private static readonly string DayD = AttendanceAccessSeed.DayD.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // Callers (see TestUsers). Properties, not fields: each TestUsers call gives a fresh unlinked user id.
    private static TestUser EmployeeE1 => TestUsers.Employee(TestSeed.E1);
    private static TestUser AttUserE1 => TestUsers.LinkedTo(TestSeed.E1, AttendanceAccessSeed.AttendanceUserRole);
    private static TestUser ManagerM1 => TestUsers.Manager(TestSeed.M1);
    private static TestUser EmployeeRoleM1 => TestUsers.Employee(TestSeed.M1);
    private static TestUser OfficerE2 => TestUsers.LinkedTo(TestSeed.E2, AttendanceAccessSeed.AttendanceOfficerRole);
    private static TestUser OfficerUnlinked => TestUsers.Unlinked(AttendanceAccessSeed.AttendanceOfficerRole);
    private static TestUser ReportsOnlyE1 => TestUsers.LinkedTo(TestSeed.E1, AttendanceAccessSeed.AttendanceReportsOnlyRole);

    [OneTimeSetUp]
    public Task AddAttendanceRowsAsync()
        => Factory.ChangeDatabaseAsync(AttendanceAccessSeed.AddRowsAsync);

    private static IEnumerable<TestCaseData> StatusCases()
    {
        var attUser = AttUserE1;
        var manager = ManagerM1;
        var officer = OfficerE2;
        var officerUnlinked = OfficerUnlinked;
        var reportsOnly = ReportsOnlyE1;
        var admin = TestUsers.Admin;

        // B07-02: the leak in C1, an employee closing someone else's session.
        yield return Case("B07-02", EmployeeE1, HttpMethod.Post, CheckOut, HttpStatusCode.Forbidden, new CheckOutRequestModel { EmployeeId = E2 });

        // B07-03 and AC7: a manager cannot clock in for a report.
        yield return Case("B07-03", manager, HttpMethod.Post, CheckIn, HttpStatusCode.Forbidden, new CheckInRequestModel { EmployeeId = E1 });

        // B07-04: signed in, not linked, not an attendance admin.
        yield return Case("B07-04", TestUsers.NoRole, HttpMethod.Post, CheckIn, HttpStatusCode.Forbidden, new CheckInRequestModel());

        // B07-05: not signed in.
        yield return Case("B07-05", TestUsers.Anonymous, HttpMethod.Post, CheckIn, HttpStatusCode.Unauthorized, new CheckInRequestModel());
        yield return Case("B07-05", TestUsers.Anonymous, HttpMethod.Get, Records(E1), HttpStatusCode.Unauthorized);
        yield return Case("B07-05", TestUsers.Anonymous, HttpMethod.Get, Daily(O1), HttpStatusCode.Unauthorized);

        // B07-14: attendance admins (role granted attendance.admin, unlinked; and an admin) can add manual entries.
        yield return Case("B07-14", officerUnlinked, HttpMethod.Post, ManualEntry, HttpStatusCode.OK, NewManualEntry(E1, slot: 0));
        yield return Case("B07-14", admin, HttpMethod.Post, ManualEntry, HttpStatusCode.OK, NewManualEntry(E1, slot: 1));

        // B07-15: self reads need no menu at all.
        yield return Case("B07-15", EmployeeE1, HttpMethod.Get, Records(E1), HttpStatusCode.OK);
        yield return Case("B07-15", EmployeeE1, HttpMethod.Get, Summary(E1), HttpStatusCode.OK);

        // B07-16: the attendance page menu and the reports menus grant nothing over others.
        foreach (var user in new[] { attUser, reportsOnly })
        {
            yield return Case("B07-16", user, HttpMethod.Get, Records(E2), HttpStatusCode.Forbidden);
            yield return Case("B07-16", user, HttpMethod.Get, Summary(E2), HttpStatusCode.Forbidden);
        }

        // B07-17 and B07-18: the direct manager reads a report's attendance, not anyone else's.
        yield return Case("B07-17", manager, HttpMethod.Get, Records(E1), HttpStatusCode.OK);
        yield return Case("B07-17", manager, HttpMethod.Get, Summary(E1), HttpStatusCode.OK);
        yield return Case("B07-18", manager, HttpMethod.Get, Records(E2), HttpStatusCode.Forbidden);
        yield return Case("B07-18", manager, HttpMethod.Get, Summary(E2), HttpStatusCode.Forbidden);

        // B07-19: being E1's ManagerId without the manager.team menu is not enough.
        yield return Case("B07-19", EmployeeRoleM1, HttpMethod.Get, Records(E1), HttpStatusCode.Forbidden);

        // B07-20 and B07-21: an unlinked non-admin gets nothing, and cannot probe for ids that do not exist.
        yield return Case("B07-20", TestUsers.NoRole, HttpMethod.Get, Records(E1), HttpStatusCode.Forbidden);
        yield return Case("B07-21", TestUsers.NoRole, HttpMethod.Get, Records(MissingEmployeeId), HttpStatusCode.Forbidden);

        // B07-22: attendance admins read anyone's records.
        foreach (var user in new[] { officer, admin })
        {
            yield return Case("B07-22", user, HttpMethod.Get, Records(E1), HttpStatusCode.OK);
            yield return Case("B07-22", user, HttpMethod.Get, Summary(E1), HttpStatusCode.OK);
        }

        // B07-23: an employee cannot ask any report for another employee.
        foreach (var path in ReportPaths(O1, employeeId: E2))
            yield return Case("B07-23", attUser, HttpMethod.Get, path, HttpStatusCode.Forbidden);

        // B07-24: nor for another organisation.
        foreach (var path in ReportPaths(AttendanceAccessSeed.OrganizationO2))
            yield return Case("B07-24", attUser, HttpMethod.Get, path, HttpStatusCode.Forbidden);

        // B07-25: a manager can ask for a report, not for someone outside the team.
        foreach (var path in ReportPaths(O1, employeeId: E2))
            yield return Case("B07-25", manager, HttpMethod.Get, path, HttpStatusCode.Forbidden);
        foreach (var path in ReportPaths(O1, employeeId: E1))
            yield return Case("B07-25", manager, HttpMethod.Get, path, HttpStatusCode.OK);

        // B07-26: an unlinked non-admin gets no reports.
        yield return Case("B07-26", TestUsers.NoRole, HttpMethod.Get, Daily(O1), HttpStatusCode.Forbidden);
        yield return Case("B07-26", TestUsers.NoRole, HttpMethod.Get, Export(O1, "csv"), HttpStatusCode.Forbidden);

        // B07-27: attendance admins get every report and export.
        foreach (var user in new[] { officerUnlinked, admin })
        {
            foreach (var path in ReportPaths(O1))
                yield return Case("B07-27", user, HttpMethod.Get, path, HttpStatusCode.OK);
        }
    }

    [TestCaseSource(nameof(StatusCases))]
    public async Task Request_ReturnsExpectedStatus(
        TestUser user, HttpMethod method, string path, HttpStatusCode expected, object? body)
    {
        await ExpectStatusAsync(user, method, path, expected, body);
    }

    [Test]
    public async Task B07_01_EmployeeActsOnAnotherEmployee_IsRefusedAndNothingChanges()
    {
        var user = AttUserE1;
        var e1Before = await RecordCountAsync(E1);
        var e2Before = await RecordCountAsync(E2);

        await ExpectStatusAsync(user, HttpMethod.Post, CheckIn, HttpStatusCode.Forbidden, new CheckInRequestModel { EmployeeId = E2 });
        await ExpectStatusAsync(user, HttpMethod.Post, CheckOut, HttpStatusCode.Forbidden, new CheckOutRequestModel { EmployeeId = E2 });
        await ExpectStatusAsync(user, HttpMethod.Post, BreakStart, HttpStatusCode.Forbidden, new StartBreakRequestModel { EmployeeId = E2 });
        await ExpectStatusAsync(user, HttpMethod.Post, BreakEnd, HttpStatusCode.Forbidden, new EndBreakRequestModel { EmployeeId = E2 });

        var session = await Factory.QueryDatabaseAsync(db => db.AttendanceRecords.AsNoTracking()
            .Where(r => r.Id == AttendanceAccessSeed.OpenSessionE2)
            .Select(r => new { r.CheckOutAtUtc, Breaks = r.Breaks.Count })
            .SingleAsync());
        var e1After = await RecordCountAsync(E1);
        var e2After = await RecordCountAsync(E2);

        Assert.Multiple(() =>
        {
            Assert.That(session.CheckOutAtUtc, Is.Null, $"{user.Description}: E2's open session should still have no check-out.");
            Assert.That(session.Breaks, Is.Zero, $"{user.Description}: E2's open session should still have no break.");
            Assert.That(e1After, Is.EqualTo(e1Before), $"{user.Description}: no record should be created for E1.");
            Assert.That(e2After, Is.EqualTo(e2Before), $"{user.Description}: no record should be created for E2.");
        });
    }

    [Test]
    public async Task B07_06_EmployeeSelfFlow_UsesLinkedEmployeeAndServerTimes()
    {
        var user = TestUsers.LinkedTo(AttendanceAccessSeed.S1, AttendanceAccessSeed.AttendanceUserRole);
        var s1 = AttendanceAccessSeed.S1.EmployeeId;
        var forgedTime = new DateTime(2020, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        var checkInBody = await ExpectStatusAsync(user, HttpMethod.Post, CheckIn, HttpStatusCode.OK,
            new CheckInRequestModel { CheckInAtUtc = forgedTime });
        await ExpectStatusAsync(user, HttpMethod.Post, BreakStart, HttpStatusCode.OK, new StartBreakRequestModel { StartAtUtc = forgedTime });
        await ExpectStatusAsync(user, HttpMethod.Post, BreakEnd, HttpStatusCode.OK, new EndBreakRequestModel { EndAtUtc = forgedTime });
        await ExpectStatusAsync(user, HttpMethod.Post, CheckOut, HttpStatusCode.OK, new CheckOutRequestModel { CheckOutAtUtc = forgedTime });

        var json = ParseJson(checkInBody, user, HttpMethod.Post, CheckIn);
        var recordId = json.GetProperty("id").GetInt32();
        var record = await Factory.QueryDatabaseAsync(db => db.AttendanceRecords.AsNoTracking()
            .Include(r => r.Breaks)
            .SingleAsync(r => r.Id == recordId));

        Assert.Multiple(() =>
        {
            Assert.That(record.EmployeeId, Is.EqualTo(s1), "The record should belong to S1.");
            Assert.That(record.CheckInAtUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)),
                "The check-in time should be the server's, not the 2020 time sent.");
            Assert.That(record.CheckOutAtUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)),
                "The check-out time should be the server's.");
            Assert.That(record.Breaks, Has.Count.EqualTo(1));
            Assert.That(record.Breaks.Single().StartAtUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
            Assert.That(record.Breaks.Single().EndAtUtc, Is.Not.Null);
        });
    }

    [Test]
    public async Task B07_07_EmployeeChecksInWithOwnId_IsAllowed()
    {
        var user = TestUsers.LinkedTo(AttendanceAccessSeed.S2, AttendanceAccessSeed.AttendanceUserRole);
        var s2 = AttendanceAccessSeed.S2.EmployeeId;

        var body = await ExpectStatusAsync(user, HttpMethod.Post, CheckIn, HttpStatusCode.OK, new CheckInRequestModel { EmployeeId = s2 });

        AssertEmployeeId(body, user, CheckIn, s2);
    }

    [Test]
    public async Task B07_08_AttendanceAdminChecksAnotherEmployeeInAndOut_IsAllowed()
    {
        var user = OfficerE2;
        var s3 = AttendanceAccessSeed.S3.EmployeeId;

        var inBody = await ExpectStatusAsync(user, HttpMethod.Post, CheckIn, HttpStatusCode.OK, new CheckInRequestModel { EmployeeId = s3 });
        var outBody = await ExpectStatusAsync(user, HttpMethod.Post, CheckOut, HttpStatusCode.OK, new CheckOutRequestModel { EmployeeId = s3 });

        AssertEmployeeId(inBody, user, CheckIn, s3);
        AssertEmployeeId(outBody, user, CheckOut, s3);
    }

    [Test]
    public async Task B07_09_UnlinkedAdminChecksInWithoutChoosing_Gets400AndNothingIsCreated()
    {
        var user = TestUsers.Admin;
        var before = await Factory.QueryDatabaseAsync(db => db.AttendanceRecords.CountAsync());

        await ExpectStatusAsync(user, HttpMethod.Post, CheckIn, HttpStatusCode.BadRequest, new CheckInRequestModel());

        var after = await Factory.QueryDatabaseAsync(db => db.AttendanceRecords.CountAsync());
        Assert.That(after, Is.EqualTo(before), $"{user.Description}: no attendance record should be created.");
    }

    [Test]
    public async Task B07_10_AdminChecksOutAnotherEmployee_WithSuppliedTime_IsAllowed()
    {
        var user = TestUsers.Admin;
        var checkOutAt = AttendanceAccessSeed.OpenSessionM1CheckIn.AddHours(9);

        await ExpectStatusAsync(user, HttpMethod.Post, CheckOut, HttpStatusCode.OK,
            new CheckOutRequestModel { EmployeeId = M1, CheckOutAtUtc = checkOutAt });

        var closedAt = await Factory.QueryDatabaseAsync(db => db.AttendanceRecords.AsNoTracking()
            .Where(r => r.Id == AttendanceAccessSeed.OpenSessionM1)
            .Select(r => r.CheckOutAtUtc)
            .SingleAsync());
        Assert.That(closedAt, Is.EqualTo(checkOutAt), "M1's session should be closed at the time the admin supplied.");
    }

    [Test]
    public async Task B07_11_to_13_ManualEntryByNonAttendanceAdmin_IsRefusedAndCreatesNothing()
    {
        var before = await Factory.QueryDatabaseAsync(db => db.AttendanceRecords.CountAsync());

        // B07-11: an employee for themselves; B07-12: for someone else; B07-13: a manager and a reports-only role.
        await ExpectStatusAsync(EmployeeE1, HttpMethod.Post, ManualEntry, HttpStatusCode.Forbidden, NewManualEntry(E1, slot: 10));
        await ExpectStatusAsync(AttUserE1, HttpMethod.Post, ManualEntry, HttpStatusCode.Forbidden, NewManualEntry(E2, slot: 11));
        await ExpectStatusAsync(ManagerM1, HttpMethod.Post, ManualEntry, HttpStatusCode.Forbidden, NewManualEntry(E1, slot: 12));
        await ExpectStatusAsync(ReportsOnlyE1, HttpMethod.Post, ManualEntry, HttpStatusCode.Forbidden, NewManualEntry(E1, slot: 13));

        var after = await Factory.QueryDatabaseAsync(db => db.AttendanceRecords.CountAsync());
        Assert.That(after, Is.EqualTo(before), "No manual attendance record should be created.");
    }

    [Test]
    public async Task B07_21_AttendanceAdminAsksForMissingEmployee_GetsEmptyList()
    {
        var user = OfficerE2;
        var path = Records(MissingEmployeeId);

        var body = await ExpectStatusAsync(user, HttpMethod.Get, path, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, path);

        Assert.That(json.GetArrayLength(), Is.Zero, BodyMessage(user, HttpMethod.Get, path, "an empty list", "[]", body));
    }

    [Test]
    public async Task B07_30_EmployeeDailyReport_CountsOnlyOwnRecord()
    {
        var day = await SingleDailyRowAsync(AttUserE1, Daily(O1));

        Assert.Multiple(() =>
        {
            Assert.That(day.GetProperty("totalRecords").GetInt32(), Is.EqualTo(1));
            Assert.That(day.GetProperty("presentEmployees").GetInt32(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task B07_31_ManagerDailyReport_CountsSelfAndDirectReport()
    {
        var day = await SingleDailyRowAsync(ManagerM1, Daily(O1));

        Assert.That(day.GetProperty("totalRecords").GetInt32(), Is.EqualTo(2), "M1 and E1 only.");
    }

    [Test]
    public async Task B07_32_AdminDailyReport_CountsEveryone()
    {
        var day = await SingleDailyRowAsync(TestUsers.Admin, Daily(O1));

        Assert.That(day.GetProperty("totalRecords").GetInt32(), Is.EqualTo(4), "M1, E1, E2 and X1.");
    }

    [Test]
    public async Task B07_33_DepartmentFilterOutsideScope_ReturnsNoRows()
    {
        var user = AttUserE1;
        var path = Daily(O1, departmentId: AttendanceAccessSeed.DepartmentDX);

        var body = await ExpectStatusAsync(user, HttpMethod.Get, path, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, path);
        var total = json.EnumerateArray().Sum(day => day.GetProperty("totalRecords").GetInt32());

        Assert.That(total, Is.Zero, BodyMessage(user, HttpMethod.Get, path, "totalRecords", "0", body));
    }

    [Test]
    public async Task B07_34_EmployeeAbsenteeism_UsesScopedHeadCount()
    {
        var user = AttUserE1;
        var path = Absenteeism(O1);

        var body = await ExpectStatusAsync(user, HttpMethod.Get, path, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, path);

        Assert.That(json.GetProperty("expectedWorkDays").GetInt32(), Is.EqualTo(1),
            BodyMessage(user, HttpMethod.Get, path, "expectedWorkDays", "1", body));
    }

    [Test]
    public async Task B07_35_to_37_CsvExport_HasOnlyRowsInScope()
    {
        var path = Export(O1, "csv");

        var manager = await CsvEmployeeIdsAsync(ManagerM1, path);
        var employee = await CsvEmployeeIdsAsync(AttUserE1, path);
        var admin = await CsvEmployeeIdsAsync(TestUsers.Admin, path);

        Assert.Multiple(() =>
        {
            Assert.That(manager, Is.EquivalentTo(new[] { M1, E1 }), "B07-35: the manager's export.");
            Assert.That(employee, Is.EquivalentTo(new[] { E1 }), "B07-36: the employee's export.");
            Assert.That(admin, Is.EquivalentTo(new[] { M1, E1, E2, X1 }), "B07-37: the admin's export.");
        });
    }

    [Test]
    public async Task B07_38_ManagerPeriodAndPunctuality_CountSelfAndDirectReport()
    {
        var user = ManagerM1;
        var periodPath = Period(O1);
        var punctualityPath = Punctuality(O1);

        var periodBody = await ExpectStatusAsync(user, HttpMethod.Get, periodPath, HttpStatusCode.OK);
        var punctualityBody = await ExpectStatusAsync(user, HttpMethod.Get, punctualityPath, HttpStatusCode.OK);
        var periodTotal = ParseJson(periodBody, user, HttpMethod.Get, periodPath)
            .EnumerateArray().Sum(p => p.GetProperty("totalRecords").GetInt32());
        var punctualityTotal = ParseJson(punctualityBody, user, HttpMethod.Get, punctualityPath)
            .GetProperty("totalRecords").GetInt32();

        Assert.Multiple(() =>
        {
            Assert.That(periodTotal, Is.EqualTo(2), BodyMessage(user, HttpMethod.Get, periodPath, "totalRecords", "2", periodBody));
            Assert.That(punctualityTotal, Is.EqualTo(2),
                BodyMessage(user, HttpMethod.Get, punctualityPath, "totalRecords", "2", punctualityBody));
        });
    }

    [Test]
    public async Task B07_40_AttendanceAdminMenu_IsSeededUnderAttendanceAndGrantedToAdmin()
    {
        var seeded = await Factory.QueryDatabaseAsync(async db =>
        {
            var attendanceId = await db.Menus.AsNoTracking().Where(m => m.Key == "attendance").Select(m => m.Id).SingleAsync();
            var menu = await db.Menus.AsNoTracking().SingleOrDefaultAsync(m => m.Key == "attendance.admin");
            var granted = menu is not null && await db.RoleKeyPermissions.AsNoTracking()
                .AnyAsync(p => p.RoleKey == TestSeed.AdminRoleKey && p.MenuId == menu.Id && p.Allowed);
            return new { AttendanceId = attendanceId, Menu = menu, Granted = granted };
        });

        Assert.That(seeded.Menu, Is.Not.Null, "The attendance.admin menu should be seeded.");
        Assert.Multiple(() =>
        {
            Assert.That(seeded.Menu!.ParentMenuId, Is.EqualTo(seeded.AttendanceId), "attendance.admin should be a child of attendance.");
            Assert.That(seeded.Menu.RoutePath, Is.EqualTo("/attendance"));
            Assert.That(seeded.Granted, Is.True, "ADMIN should be granted attendance.admin.");
        });
    }

    [TestCase(AttendanceAccessSeed.AttendanceUserRole, false)]
    [TestCase(AttendanceAccessSeed.AttendanceReportsOnlyRole, false)]
    [TestCase(AttendanceAccessSeed.AttendanceOfficerRole, true)]
    [TestCase("Admin", true)]
    public async Task B07_AC17_EligibilityFlag_IsTrueOnlyForAttendanceAdmins(string roleName, bool expected)
    {
        var user = roleName == "Admin" ? TestUsers.Admin : TestUsers.LinkedTo(TestSeed.E1, roleName);

        var body = await ExpectStatusAsync(user, HttpMethod.Get, Eligibility, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, Eligibility);

        bool? actual = json.TryGetProperty("canManageOtherEmployeesAttendance", out var flag)
                       && flag.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? flag.GetBoolean()
            : null;

        Assert.That(actual, Is.EqualTo(expected),
            BodyMessage(user, HttpMethod.Get, Eligibility, "canManageOtherEmployeesAttendance", expected.ToString(), body));
    }

    private async Task<JsonElement> SingleDailyRowAsync(TestUser user, string path)
    {
        var body = await ExpectStatusAsync(user, HttpMethod.Get, path, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, path);
        Assert.That(json.GetArrayLength(), Is.EqualTo(1), BodyMessage(user, HttpMethod.Get, path, "days", "1", body));
        return json[0];
    }

    private async Task<List<int>> CsvEmployeeIdsAsync(TestUser user, string path)
    {
        var body = await ExpectStatusAsync(user, HttpMethod.Get, path, HttpStatusCode.OK);
        return body
            .Split('\n')
            .Select(line => line.Trim('\r', '﻿'))
            .Skip(1)
            .Where(line => line.Length > 0)
            .Select(line => int.Parse(line.Split(',')[1], CultureInfo.InvariantCulture))
            .ToList();
    }

    private static void AssertEmployeeId(string body, TestUser user, string path, int expected)
    {
        var json = ParseJson(body, user, HttpMethod.Post, path);
        Assert.That(json.GetProperty("employeeId").GetInt32(), Is.EqualTo(expected),
            BodyMessage(user, HttpMethod.Post, path, "employeeId", expected.ToString(CultureInfo.InvariantCulture), body));
    }

    private Task<int> RecordCountAsync(int employeeId)
        => Factory.QueryDatabaseAsync(db => db.AttendanceRecords.AsNoTracking().CountAsync(r => r.EmployeeId == employeeId));

    /// <summary>A manual entry on its own day in March 2027, outside day D, so report content cases are unaffected.</summary>
    private static ManualAttendanceEntryRequestModel NewManualEntry(int employeeId, int slot)
    {
        var day = new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(slot);
        return new ManualAttendanceEntryRequestModel
        {
            OrganizationId = TestSeed.O1.Id,
            EmployeeId = employeeId,
            WorkDate = day,
            CheckInAtUtc = day.AddHours(9),
            CheckOutAtUtc = day.AddHours(17),
            ManualReason = "B07 test",
        };
    }

    private static IEnumerable<string> ReportPaths(int organizationId, int? employeeId = null)
    {
        yield return Daily(organizationId, employeeId);
        yield return Period(organizationId, employeeId);
        yield return Punctuality(organizationId, employeeId);
        yield return Absenteeism(organizationId, employeeId);
        yield return Export(organizationId, "csv", employeeId);
        yield return Export(organizationId, "xlsx", employeeId);
        yield return Export(organizationId, "pdf", employeeId);
    }

    private static string ReportQuery(int organizationId, int? employeeId, int? departmentId)
    {
        var query = $"organizationId={organizationId}&fromDate={DayD}&toDate={DayD}";
        if (employeeId.HasValue)
            query += $"&employeeId={employeeId.Value}";
        if (departmentId.HasValue)
            query += $"&departmentId={departmentId.Value}";
        return query;
    }

    private static string Daily(int organizationId, int? employeeId = null, int? departmentId = null)
        => $"/api/Attendance/reports/daily?{ReportQuery(organizationId, employeeId, departmentId)}";

    private static string Period(int organizationId, int? employeeId = null)
        => $"/api/Attendance/reports/period?{ReportQuery(organizationId, employeeId, null)}&groupBy=week";

    private static string Punctuality(int organizationId, int? employeeId = null)
        => $"/api/Attendance/analytics/punctuality?{ReportQuery(organizationId, employeeId, null)}";

    private static string Absenteeism(int organizationId, int? employeeId = null)
        => $"/api/Attendance/analytics/absenteeism?{ReportQuery(organizationId, employeeId, null)}";

    private static string Export(int organizationId, string format, int? employeeId = null)
        => $"/api/Attendance/reports/export?{ReportQuery(organizationId, employeeId, null)}&format={format}";

    private static string Records(int employeeId) => $"/api/Attendance/employee/{employeeId}";

    private static string Summary(int employeeId)
        => $"/api/Attendance/summary?employeeId={employeeId}&fromDate={DayD}&toDate={DayD}";
}
