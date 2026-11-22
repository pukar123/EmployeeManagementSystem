using System.Net;
using System.Text.Json;
using EMS.API.IntegrationTests.Harness;
using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Leave;
using EMS.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// B06: leave data is reachable only by the employee themselves, their current direct manager (read and approve
/// only) and leave admins (an admin, or a role granted the <c>leave.admin</c> menu). The self-service <c>leave</c>
/// menu grants nothing over other employees, and managers never get attachments.
/// This fixture has its own database (with <see cref="LeaveAccessSeed"/> rows) and its own attachment files,
/// which are removed again after the fixture.
/// Leave access is checked in the services, after model binding, so denied creates and updates still send a
/// valid body: the 403 then comes from the access check, not from a 400 for a missing body.
/// </summary>
[TestFixture]
public sealed class LeaveAccessTests : AccessTestFixture
{
    private static readonly int O1 = TestSeed.O1.Id;
    private static readonly int E1 = TestSeed.E1.EmployeeId;
    private static readonly int E2 = TestSeed.E2.EmployeeId;

    private static readonly string AdminSummary = $"/api/LeaveRequests/admin/summary?organizationId={O1}";
    private const string LeaveTypes = "/api/LeaveTypes";
    private const string LeaveBulkImport = "/api/LeaveImports/bulk";
    private const string LeaveRequests = "/api/LeaveRequests";
    private const string Eligibility = "/api/EmployeePortal/eligibility";

    private string _webRoot = string.Empty;

    // Callers (see TestUsers). Properties, not fields: each TestUsers call gives a fresh unlinked user id.
    private static TestUser LeaveUserE1 => TestUsers.LinkedTo(TestSeed.E1, LeaveAccessSeed.LeaveUserRole);
    private static TestUser EmployeeE1 => TestUsers.Employee(TestSeed.E1);
    private static TestUser ManagerM1 => TestUsers.Manager(TestSeed.M1);
    private static TestUser EmployeeRoleM1 => TestUsers.Employee(TestSeed.M1);
    private static TestUser OfficerE2 => TestUsers.LinkedTo(TestSeed.E2, LeaveAccessSeed.LeaveOfficerRole);
    private static TestUser OfficerUnlinked => TestUsers.Unlinked(LeaveAccessSeed.LeaveOfficerRole);
    private static TestUser SettingsOnlyE2 => TestUsers.LinkedTo(TestSeed.E2, LeaveAccessSeed.LeaveSettingsOnlyRole);

    [OneTimeSetUp]
    public async Task AddLeaveRowsAsync()
    {
        var env = Factory.Services.GetRequiredService<IWebHostEnvironment>();
        _webRoot = LeaveAccessSeed.AttachmentRoot(env.WebRootPath, env.ContentRootPath);
        await Factory.ChangeDatabaseAsync(db => LeaveAccessSeed.AddRowsAsync(db, _webRoot));
    }

    [OneTimeTearDown]
    public void DeleteAttachmentFiles()
    {
        if (!string.IsNullOrEmpty(_webRoot))
            LeaveAccessSeed.DeleteFiles(_webRoot);
    }

    private static IEnumerable<TestCaseData> StatusCases()
    {
        var leaveUser = LeaveUserE1;
        var manager = ManagerM1;
        var officer = OfficerE2;
        var admin = TestUsers.Admin;

        // B06-01: a plain employee with the leave menu cannot read another employee's leave.
        yield return Case("B06-01", leaveUser, HttpMethod.Get, Balances(E2), HttpStatusCode.Forbidden);
        yield return Case("B06-01", leaveUser, HttpMethod.Get, Balance(E2), HttpStatusCode.Forbidden);
        yield return Case("B06-01", leaveUser, HttpMethod.Get, RequestsOf(E2), HttpStatusCode.Forbidden);
        yield return Case("B06-01", leaveUser, HttpMethod.Get, Request(LeaveAccessSeed.RequestE2Read), HttpStatusCode.Forbidden);

        // B06-04: self-service reads.
        yield return Case("B06-04", leaveUser, HttpMethod.Get, Balances(E1), HttpStatusCode.OK);
        yield return Case("B06-04", leaveUser, HttpMethod.Get, RequestsOf(E1), HttpStatusCode.OK);
        yield return Case("B06-04", leaveUser, HttpMethod.Get, Request(LeaveAccessSeed.RequestE1Read), HttpStatusCode.OK);
        yield return Case("B06-04", leaveUser, HttpMethod.Get, AttachmentsOf(LeaveAccessSeed.RequestE1Read), HttpStatusCode.OK);
        yield return Case("B06-04", leaveUser, HttpMethod.Get, AttachmentFile(LeaveAccessSeed.AttachmentE1), HttpStatusCode.OK);

        // B06-05: self-service changes (the upload is its own test below).
        yield return Case("B06-05", leaveUser, HttpMethod.Post, LeaveRequests, HttpStatusCode.Created, NewRequest(E1, slot: 0));
        yield return Case("B06-05", leaveUser, HttpMethod.Put, Request(LeaveAccessSeed.RequestE1UpdateSelf), HttpStatusCode.OK,
            UpdateFor(LeaveAccessSeed.RequestE1UpdateSelf));
        yield return Case("B06-05", leaveUser, HttpMethod.Post, Cancel(LeaveAccessSeed.RequestE1CancelSelf), HttpStatusCode.OK);
        yield return Case("B06-05", leaveUser, HttpMethod.Delete, Attachment(LeaveAccessSeed.AttachmentE1DeleteSelf), HttpStatusCode.NoContent);

        // B06-06: self needs no menu at all.
        yield return Case("B06-06", EmployeeE1, HttpMethod.Get, Balances(E1), HttpStatusCode.OK);
        yield return Case("B06-06", EmployeeE1, HttpMethod.Get, RequestsOf(E1), HttpStatusCode.OK);

        // B06-07: the direct manager can read a report's requests and balances.
        yield return Case("B06-07", manager, HttpMethod.Get, Balances(E1), HttpStatusCode.OK);
        yield return Case("B06-07", manager, HttpMethod.Get, Balance(E1), HttpStatusCode.OK);
        yield return Case("B06-07", manager, HttpMethod.Get, RequestsOf(E1), HttpStatusCode.OK);
        yield return Case("B06-07", manager, HttpMethod.Get, Request(LeaveAccessSeed.RequestE1Read), HttpStatusCode.OK);

        // B06-09: approval by the direct manager is unchanged.
        yield return Case("B06-09", manager, HttpMethod.Post, Approve(LeaveAccessSeed.RequestE1ApproveManager), HttpStatusCode.OK,
            new ApproveLeaveRequestRequestModel());

        // B06-10: a manager gets nothing for someone outside the team.
        yield return Case("B06-10", manager, HttpMethod.Get, Balances(E2), HttpStatusCode.Forbidden);
        yield return Case("B06-10", manager, HttpMethod.Get, RequestsOf(E2), HttpStatusCode.Forbidden);
        yield return Case("B06-10", manager, HttpMethod.Get, Request(LeaveAccessSeed.RequestE2Read), HttpStatusCode.Forbidden);
        yield return Case("B06-10", manager, HttpMethod.Get, AttachmentsOf(LeaveAccessSeed.RequestE2Read), HttpStatusCode.Forbidden);

        // B06-11: being E1's ManagerId without the manager.team menu is not enough.
        yield return Case("B06-11", EmployeeRoleM1, HttpMethod.Get, Balances(E1), HttpStatusCode.Forbidden);
        yield return Case("B06-11", EmployeeRoleM1, HttpMethod.Get, RequestsOf(E1), HttpStatusCode.Forbidden);

        // B06-12: a linked leave admin can do everything on someone else's leave (the upload is its own test below).
        yield return Case("B06-12", officer, HttpMethod.Get, Balances(E1), HttpStatusCode.OK);
        yield return Case("B06-12", officer, HttpMethod.Get, Balance(E1), HttpStatusCode.OK);
        yield return Case("B06-12", officer, HttpMethod.Get, RequestsOf(E1), HttpStatusCode.OK);
        yield return Case("B06-12", officer, HttpMethod.Get, Request(LeaveAccessSeed.RequestE1Read), HttpStatusCode.OK);
        yield return Case("B06-12", officer, HttpMethod.Post, LeaveRequests, HttpStatusCode.Created, NewRequest(E1, slot: 1));
        yield return Case("B06-12", officer, HttpMethod.Put, Request(LeaveAccessSeed.RequestE1UpdateOfficer), HttpStatusCode.OK,
            UpdateFor(LeaveAccessSeed.RequestE1UpdateOfficer));
        yield return Case("B06-12", officer, HttpMethod.Post, Cancel(LeaveAccessSeed.RequestE1CancelOfficer), HttpStatusCode.OK);
        yield return Case("B06-12", officer, HttpMethod.Get, AttachmentsOf(LeaveAccessSeed.RequestE1Read), HttpStatusCode.OK);
        yield return Case("B06-12", officer, HttpMethod.Get, AttachmentFile(LeaveAccessSeed.AttachmentE1), HttpStatusCode.OK);
        yield return Case("B06-12", officer, HttpMethod.Delete, Attachment(LeaveAccessSeed.AttachmentE1DeleteOfficer), HttpStatusCode.NoContent);

        // B06-13: a linked leave admin can approve someone else's request (their own is the test below).
        yield return Case("B06-13", officer, HttpMethod.Post, Approve(LeaveAccessSeed.RequestE1ApproveOfficer), HttpStatusCode.OK,
            new ApproveLeaveRequestRequestModel());

        // B06-14: an unlinked leave admin can read (the approval refusal is the test below).
        yield return Case("B06-14", OfficerUnlinked, HttpMethod.Get, Balances(E1), HttpStatusCode.OK);
        yield return Case("B06-14", OfficerUnlinked, HttpMethod.Get, Request(LeaveAccessSeed.RequestE1Read), HttpStatusCode.OK);

        // B06-15: an unlinked admin can read and change anyone's leave.
        yield return Case("B06-15", admin, HttpMethod.Get, Balances(E2), HttpStatusCode.OK);
        yield return Case("B06-15", admin, HttpMethod.Get, RequestsOf(E2), HttpStatusCode.OK);
        yield return Case("B06-15", admin, HttpMethod.Get, AttachmentFile(LeaveAccessSeed.AttachmentE2), HttpStatusCode.OK);
        yield return Case("B06-15", admin, HttpMethod.Post, Cancel(LeaveAccessSeed.RequestE1CancelAdmin), HttpStatusCode.OK);

        // B06-16: signed in, not linked, not a leave admin.
        yield return Case("B06-16", TestUsers.NoRole, HttpMethod.Get, Balances(E1), HttpStatusCode.Forbidden);
        yield return Case("B06-16", TestUsers.NoRole, HttpMethod.Get, Request(LeaveAccessSeed.RequestE1Read), HttpStatusCode.Forbidden);
        yield return Case("B06-16", TestUsers.NoRole, HttpMethod.Get, AttachmentsOf(LeaveAccessSeed.RequestE1Read), HttpStatusCode.Forbidden);

        // B06-17: not signed in.
        yield return Case("B06-17", TestUsers.Anonymous, HttpMethod.Get, Balances(E1), HttpStatusCode.Unauthorized);

        // B06-18 and B06-19: the admin summary needs the LeaveAdmin policy.
        yield return Case("B06-18", leaveUser, HttpMethod.Get, AdminSummary, HttpStatusCode.Forbidden);
        yield return Case("B06-18", manager, HttpMethod.Get, AdminSummary, HttpStatusCode.Forbidden);
        yield return Case("B06-18", SettingsOnlyE2, HttpMethod.Get, AdminSummary, HttpStatusCode.Forbidden);
        yield return Case("B06-19", officer, HttpMethod.Get, AdminSummary, HttpStatusCode.OK);
        yield return Case("B06-19", admin, HttpMethod.Get, AdminSummary, HttpStatusCode.OK);

        // B06-20 and B06-21: the LeaveAdmin policy means "admin or leave.admin menu", nothing less.
        yield return Case("B06-20", officer, HttpMethod.Post, LeaveTypes, HttpStatusCode.Created,
            new CreateLeaveTypeRequestModel { OrganizationId = O1, Name = "B06 Officer Leave", IsActive = true });
        yield return Case("B06-20", officer, HttpMethod.Post, LeaveBulkImport, HttpStatusCode.OK,
            new BulkLeaveImportRequestModel { OrganizationId = O1, Items = [] });
        yield return Case("B06-21", SettingsOnlyE2, HttpMethod.Post, LeaveTypes, HttpStatusCode.Forbidden);
        yield return Case("B06-21", leaveUser, HttpMethod.Post, LeaveTypes, HttpStatusCode.Forbidden);
    }

    [TestCaseSource(nameof(StatusCases))]
    public async Task Request_ReturnsExpectedStatus(
        TestUser user, HttpMethod method, string path, HttpStatusCode expected, object? body)
    {
        await ExpectStatusAsync(user, method, path, expected, body);
    }

    [Test]
    public async Task B06_02_PlainEmployeeChangesAnotherEmployeesLeave_IsRefusedAndNothingChanges()
    {
        var user = LeaveUserE1;
        var before = await RequestsSnapshotAsync(E2);

        await ExpectStatusAsync(user, HttpMethod.Post, LeaveRequests, HttpStatusCode.Forbidden, NewRequest(E2, slot: 10));
        await ExpectStatusAsync(user, HttpMethod.Put, Request(LeaveAccessSeed.RequestE2Read), HttpStatusCode.Forbidden,
            UpdateFor(LeaveAccessSeed.RequestE2Read));
        await ExpectStatusAsync(user, HttpMethod.Post, Cancel(LeaveAccessSeed.RequestE2Read), HttpStatusCode.Forbidden);

        var after = await RequestsSnapshotAsync(E2);
        Assert.That(after, Is.EqualTo(before),
            $"{user.Description}: E2's leave requests (id, status, dates) should be unchanged after the refused calls.");
    }

    [Test]
    public async Task B06_03_PlainEmployeeUsesAnotherEmployeesAttachments_IsRefusedAndNothingIsStored()
    {
        var user = LeaveUserE1;
        var request = LeaveAccessSeed.RequestE2Read;
        var rowsBefore = await AttachmentRowCountAsync(request);
        var filesBefore = FileCount(request);

        await ExpectStatusAsync(user, HttpMethod.Get, AttachmentsOf(request), HttpStatusCode.Forbidden);
        await ExpectStatusAsync(user, HttpMethod.Get, AttachmentFile(LeaveAccessSeed.AttachmentE2), HttpStatusCode.Forbidden);
        await ExpectStatusAsync(user, HttpMethod.Delete, Attachment(LeaveAccessSeed.AttachmentE2), HttpStatusCode.Forbidden);
        await ExpectUploadStatusAsync(user, request, HttpStatusCode.Forbidden);

        await AssertAttachmentE2StillStoredAsync(user);
        var rowsAfter = await AttachmentRowCountAsync(request);
        Assert.Multiple(() =>
        {
            Assert.That(rowsAfter, Is.EqualTo(rowsBefore),
                $"{user.Description}: no attachment row should be added to R_E2_Read.");
            Assert.That(FileCount(request), Is.EqualTo(filesBefore),
                $"{user.Description}: no file should be left in R_E2_Read's attachment folder.");
        });
    }

    [Test]
    public async Task B06_05_EmployeeUploadsToOwnRequest_IsAllowed()
    {
        await ExpectUploadStatusAsync(LeaveUserE1, LeaveAccessSeed.RequestE1UpdateSelf, HttpStatusCode.OK);
    }

    [Test]
    public async Task B06_08_DirectManagerChangesOrUsesAttachmentsOfReport_IsRefusedAndNothingChanges()
    {
        var user = ManagerM1;
        var request = LeaveAccessSeed.RequestE1Read;
        var rowsBefore = await AttachmentRowCountAsync(request);
        var filesBefore = FileCount(request);

        await ExpectStatusAsync(user, HttpMethod.Post, LeaveRequests, HttpStatusCode.Forbidden, NewRequest(E1, slot: 20));
        await ExpectStatusAsync(user, HttpMethod.Put, Request(request), HttpStatusCode.Forbidden, UpdateFor(request));
        await ExpectStatusAsync(user, HttpMethod.Post, Cancel(request), HttpStatusCode.Forbidden);
        await ExpectStatusAsync(user, HttpMethod.Get, AttachmentsOf(request), HttpStatusCode.Forbidden);
        await ExpectStatusAsync(user, HttpMethod.Get, AttachmentFile(LeaveAccessSeed.AttachmentE1), HttpStatusCode.Forbidden);
        await ExpectStatusAsync(user, HttpMethod.Delete, Attachment(LeaveAccessSeed.AttachmentE1), HttpStatusCode.Forbidden);
        await ExpectUploadStatusAsync(user, request, HttpStatusCode.Forbidden);

        var status = await Factory.QueryDatabaseAsync(db => db.LeaveRequests.AsNoTracking()
            .Where(r => r.Id == request).Select(r => r.Status).SingleAsync());
        var attachmentStillThere = await Factory.QueryDatabaseAsync(db => db.LeaveRequestAttachments.AsNoTracking()
            .AnyAsync(a => a.Id == LeaveAccessSeed.AttachmentE1));
        var rowsAfter = await AttachmentRowCountAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(LeaveRequestStatus.Pending), $"{user.Description}: R_E1_Read should still be pending.");
            Assert.That(attachmentStillThere, Is.True, $"{user.Description}: A_E1 should still exist.");
            Assert.That(File.Exists(PhysicalPathOf(request, LeaveAccessSeed.AttachmentE1)), Is.True,
                $"{user.Description}: A_E1's file should still exist.");
            Assert.That(rowsAfter, Is.EqualTo(rowsBefore), $"{user.Description}: no attachment row should be added to R_E1_Read.");
            Assert.That(FileCount(request), Is.EqualTo(filesBefore), $"{user.Description}: no file should be left for R_E1_Read.");
        });
    }

    [Test]
    public async Task B06_12_LeaveAdminUploadsToAnotherEmployeesRequest_IsAllowed()
    {
        await ExpectUploadStatusAsync(OfficerE2, LeaveAccessSeed.RequestE1Read, HttpStatusCode.OK);
    }

    [Test]
    public async Task B06_13_LeaveAdminApprovesOwnRequest_IsRefusedAsSelfDecision()
    {
        await ExpectApprovalRefusedAsync(OfficerE2, LeaveAccessSeed.RequestE2Own, LeaveApprovalMessages.SelfDecisionDenied);
    }

    [Test]
    public async Task B06_14_UnlinkedLeaveAdminApproves_IsRefusedAsNotLinked()
    {
        await ExpectApprovalRefusedAsync(OfficerUnlinked, LeaveAccessSeed.RequestE1Read, LeaveApprovalMessages.ReviewerNotLinked);
    }

    [TestCase(LeaveAccessSeed.LeaveUserRole, false)]
    [TestCase(LeaveAccessSeed.LeaveOfficerRole, true)]
    [TestCase("Admin", true)]
    public async Task B06_22_EligibilityFlag_IsTrueOnlyForLeaveAdmins(string roleName, bool expected)
    {
        var user = roleName == "Admin" ? TestUsers.Admin : TestUsers.LinkedTo(
            roleName == LeaveAccessSeed.LeaveUserRole ? TestSeed.E1 : TestSeed.E2, roleName);

        var body = await ExpectStatusAsync(user, HttpMethod.Get, Eligibility, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, Eligibility);

        bool? actual = json.TryGetProperty("canManageOtherEmployeesLeave", out var flag)
                       && flag.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? flag.GetBoolean()
            : null;

        Assert.That(actual, Is.EqualTo(expected),
            BodyMessage(user, HttpMethod.Get, Eligibility, "canManageOtherEmployeesLeave", expected.ToString(), body));
    }

    private async Task ExpectApprovalRefusedAsync(TestUser user, int requestId, string expectedMessage)
    {
        var path = Approve(requestId);
        var body = await ExpectStatusAsync(user, HttpMethod.Post, path, HttpStatusCode.Forbidden, new ApproveLeaveRequestRequestModel());
        var json = ParseJson(body, user, HttpMethod.Post, path);
        var message = json.TryGetProperty("message", out var value) ? value.GetString() : null;

        var status = await Factory.QueryDatabaseAsync(db => db.LeaveRequests.AsNoTracking()
            .Where(r => r.Id == requestId).Select(r => r.Status).SingleAsync());

        Assert.Multiple(() =>
        {
            Assert.That(message, Is.EqualTo(expectedMessage),
                BodyMessage(user, HttpMethod.Post, path, "message", expectedMessage, body));
            Assert.That(status, Is.EqualTo(LeaveRequestStatus.Pending), $"{user.Description} -> POST {path}: the request should still be pending.");
        });
    }

    private async Task ExpectUploadStatusAsync(TestUser user, int leaveRequestId, HttpStatusCode expected)
    {
        const string path = "/api/LeaveAttachments";
        using var client = Factory.CreateClientAs(user);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(leaveRequestId.ToString(System.Globalization.CultureInfo.InvariantCulture)), "leaveRequestId");
        var file = new ByteArrayContent("B06 uploaded evidence"u8.ToArray());
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", "b06-upload.txt");

        using var response = await client.PostAsync(path, form);
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(expected),
            $"{user.Description} -> POST {path} (upload to request {leaveRequestId}): expected {(int)expected} {expected}, " +
            $"got {(int)response.StatusCode} {response.StatusCode}. Body starts: {(body.Length > 400 ? body[..400] : body)}");
    }

    private async Task AssertAttachmentE2StillStoredAsync(TestUser user)
    {
        var rowExists = await Factory.QueryDatabaseAsync(db => db.LeaveRequestAttachments.AsNoTracking()
            .AnyAsync(a => a.Id == LeaveAccessSeed.AttachmentE2));

        Assert.Multiple(() =>
        {
            Assert.That(rowExists, Is.True, $"{user.Description}: A_E2 should still exist after a refused delete.");
            Assert.That(File.Exists(PhysicalPathOf(LeaveAccessSeed.RequestE2Read, LeaveAccessSeed.AttachmentE2)), Is.True,
                $"{user.Description}: A_E2's file should still exist after a refused delete.");
        });
    }

    private Task<List<string>> RequestsSnapshotAsync(int employeeId)
        => Factory.QueryDatabaseAsync(db => db.LeaveRequests.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderBy(r => r.Id)
            .Select(r => $"{r.Id}:{r.Status}:{r.StartDateUtc:yyyy-MM-dd}:{r.EndDateUtc:yyyy-MM-dd}:{r.RequestedAmount}")
            .ToListAsync());

    private Task<int> AttachmentRowCountAsync(int leaveRequestId)
        => Factory.QueryDatabaseAsync(db => db.LeaveRequestAttachments.AsNoTracking()
            .CountAsync(a => a.LeaveRequestId == leaveRequestId));

    private int FileCount(int leaveRequestId)
    {
        var folder = Path.Combine(_webRoot, "attachments", "Leave", leaveRequestId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Directory.Exists(folder) ? Directory.GetFiles(folder).Length : 0;
    }

    private string PhysicalPathOf(int requestId, int attachmentId)
        => LeaveAccessSeed.PhysicalPath(_webRoot, LeaveAccessSeed.AttachmentRelativePath(requestId, attachmentId));

    private static CreateLeaveRequestRequestModel NewRequest(int employeeId, int slot) => new()
    {
        EmployeeId = employeeId,
        LeaveTypeId = LeaveAccessSeed.LeaveTypeId,
        StartDateUtc = LeaveAccessSeed.FreeDay(slot),
        EndDateUtc = LeaveAccessSeed.FreeDay(slot),
        Unit = LeaveUnit.Days,
        RequestedAmount = 1m,
        Reason = "B06 test",
    };

    private static UpdateLeaveRequestRequestModel UpdateFor(int requestId) => new()
    {
        StartDateUtc = LeaveAccessSeed.DayOf(requestId),
        EndDateUtc = LeaveAccessSeed.DayOf(requestId),
        Unit = LeaveUnit.Days,
        RequestedAmount = 1m,
        Reason = "B06 updated",
    };

    private static string Balances(int employeeId) => $"/api/LeaveBalances/employee/{employeeId}";
    private static string Balance(int employeeId) => $"/api/LeaveBalances/employee/{employeeId}/type/{LeaveAccessSeed.LeaveTypeId}";
    private static string RequestsOf(int employeeId) => $"/api/LeaveRequests/employee/{employeeId}";
    private static string Request(int requestId) => $"/api/LeaveRequests/{requestId}";
    private static string Cancel(int requestId) => $"/api/LeaveRequests/{requestId}/cancel";
    private static string Approve(int requestId) => $"/api/LeaveRequests/{requestId}/approve";
    private static string AttachmentsOf(int requestId) => $"/api/LeaveAttachments/request/{requestId}";
    private static string Attachment(int attachmentId) => $"/api/LeaveAttachments/{attachmentId}";
    private static string AttachmentFile(int attachmentId) => $"/api/LeaveAttachments/{attachmentId}/file";
}
