using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EMS.API.IntegrationTests.Harness;
using EMS.Application.DTOs.Document;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// B09: employee documents are for the employee, their current direct manager (read-only, with the
/// <c>manager.team</c> menu) and document admins (an admin, or a role granted the <c>documents.admin</c> menu).
/// Uploads, changes and deletes need the <c>DocumentAdmin</c> policy, which refuses an upload before any file is
/// written. Listing every document is for document admins only, and every list is paged in the database.
/// Unknown ids are 403 for everyone but document admins (404 or an empty page for them), so ids cannot be probed.
/// The <c>employees.view</c> and <c>employees.manage</c> capabilities grant nothing over documents.
/// This fixture has its own database with <see cref="DocumentAccessSeed"/> rows and its own temporary private root.
/// Every state-changing case uses its own document, so the order of tests does not matter.
/// </summary>
[TestFixture]
public sealed class DocumentAccessTests : AccessTestFixture
{
    private const string List = "/api/Documents";
    private const string Types = "/api/Documents/types";
    private const string Eligibility = "/api/EmployeePortal/eligibility";
    private const int MissingId = 999999;

    private static readonly int M1 = TestSeed.M1.EmployeeId;
    private static readonly int E1 = TestSeed.E1.EmployeeId;
    private static readonly int E2 = TestSeed.E2.EmployeeId;
    private static readonly int P1 = DocumentAccessSeed.P1.EmployeeId;

    // Callers (see TestUsers). Properties, not fields: each TestUsers call gives a fresh unlinked user id.
    private static TestUser EmployeeE1 => TestUsers.Employee(TestSeed.E1);
    private static TestUser ManagerM1 => TestUsers.Manager(TestSeed.M1);
    private static TestUser EmployeeRoleM1 => TestUsers.Employee(TestSeed.M1);
    private static TestUser OfficerE2 => TestUsers.LinkedTo(TestSeed.E2, DocumentAccessSeed.DocumentOfficerRole);
    private static TestUser OfficerUnlinked => TestUsers.Unlinked(DocumentAccessSeed.DocumentOfficerRole);
    private static TestUser DirectoryEditor => TestUsers.Unlinked(DocumentAccessSeed.DirectoryEditorRole);

    [OneTimeSetUp]
    public Task AddDocumentRowsAsync()
        => Factory.ChangeDatabaseAsync(db => DocumentAccessSeed.AddRowsAsync(db, Factory.PrivateRootPath));

    private static IEnumerable<TestCaseData> StatusCases()
    {
        var admin = TestUsers.Admin;
        var update = ValidUpdate(E1);

        // B09-01: an employee cannot read someone else's documents.
        yield return Case("B09-01", EmployeeE1, HttpMethod.Get, ByEmployee(E2), HttpStatusCode.Forbidden);
        yield return Case("B09-01", EmployeeE1, HttpMethod.Get, One(DocumentAccessSeed.DocE2Contract), HttpStatusCode.Forbidden);
        yield return Case("B09-01", EmployeeE1, HttpMethod.Get, FileOf(DocumentAccessSeed.DocE2Contract), HttpStatusCode.Forbidden);

        // B09-04: self reads.
        yield return Case("B09-04", EmployeeE1, HttpMethod.Get, ByEmployee(E1), HttpStatusCode.OK);
        yield return Case("B09-04", EmployeeE1, HttpMethod.Get, One(DocumentAccessSeed.DocE1Contract), HttpStatusCode.OK);

        // B09-05: self is read-only.
        yield return Case("B09-05", EmployeeE1, HttpMethod.Put, One(DocumentAccessSeed.DocE1Contract), HttpStatusCode.Forbidden, update);
        yield return Case("B09-05", EmployeeE1, HttpMethod.Delete, One(DocumentAccessSeed.DocE1Contract), HttpStatusCode.Forbidden);

        // B09-06: the direct manager reads a report's documents.
        yield return Case("B09-06", ManagerM1, HttpMethod.Get, ByEmployee(E1), HttpStatusCode.OK);
        yield return Case("B09-06", ManagerM1, HttpMethod.Get, One(DocumentAccessSeed.DocE1Passport), HttpStatusCode.OK);
        yield return Case("B09-06", ManagerM1, HttpMethod.Get, FileOf(DocumentAccessSeed.DocE1Passport), HttpStatusCode.OK);

        // B09-07: but not anyone else's.
        yield return Case("B09-07", ManagerM1, HttpMethod.Get, ByEmployee(E2), HttpStatusCode.Forbidden);
        yield return Case("B09-07", ManagerM1, HttpMethod.Get, One(DocumentAccessSeed.DocE2Contract), HttpStatusCode.Forbidden);
        yield return Case("B09-07", ManagerM1, HttpMethod.Get, FileOf(DocumentAccessSeed.DocE2Contract), HttpStatusCode.Forbidden);

        // B09-08: and the manager is read-only.
        yield return Case("B09-08", ManagerM1, HttpMethod.Put, One(DocumentAccessSeed.DocE1Contract), HttpStatusCode.Forbidden, update);
        yield return Case("B09-08", ManagerM1, HttpMethod.Delete, One(DocumentAccessSeed.DocE1Contract), HttpStatusCode.Forbidden);

        // B09-09: being E1's ManagerId without the manager.team menu is not enough.
        yield return Case("B09-09", EmployeeRoleM1, HttpMethod.Get, ByEmployee(E1), HttpStatusCode.Forbidden);

        // B09-10: directory rights (employees menu, employees.view, employees.manage) are not document rights.
        yield return Case("B09-10", DirectoryEditor, HttpMethod.Get, ByEmployee(E2), HttpStatusCode.Forbidden);
        yield return Case("B09-10", DirectoryEditor, HttpMethod.Get, List, HttpStatusCode.Forbidden);
        yield return Case("B09-10", DirectoryEditor, HttpMethod.Get, One(DocumentAccessSeed.DocE2Contract), HttpStatusCode.Forbidden);

        // B09-11: listing every document is for document admins only.
        foreach (var user in new[] { EmployeeE1, ManagerM1, TestUsers.NoRole })
            yield return Case("B09-11", user, HttpMethod.Get, List, HttpStatusCode.Forbidden);

        // B09-12: signed in, no role, no link.
        yield return Case("B09-12", TestUsers.NoRole, HttpMethod.Get, ByEmployee(E1), HttpStatusCode.Forbidden);
        yield return Case("B09-12", TestUsers.NoRole, HttpMethod.Get, One(DocumentAccessSeed.DocE1Contract), HttpStatusCode.Forbidden);
        yield return Case("B09-12", TestUsers.NoRole, HttpMethod.Get, FileOf(DocumentAccessSeed.DocE1Contract), HttpStatusCode.Forbidden);

        // B09-13: no id probing for non-admins.
        yield return Case("B09-13", EmployeeE1, HttpMethod.Get, One(MissingId), HttpStatusCode.Forbidden);
        yield return Case("B09-13", EmployeeE1, HttpMethod.Get, FileOf(MissingId), HttpStatusCode.Forbidden);
        yield return Case("B09-13", EmployeeE1, HttpMethod.Get, ByEmployee(MissingId), HttpStatusCode.Forbidden);

        // B09-14: document admins get 404 for an unknown document (the empty page is checked in its own test).
        yield return Case("B09-14", admin, HttpMethod.Get, One(MissingId), HttpStatusCode.NotFound);
        yield return Case("B09-14", admin, HttpMethod.Get, FileOf(MissingId), HttpStatusCode.NotFound);

        // B09-15: documents with no owner (none, or only a deleted link) are for document admins only.
        foreach (var user in new[] { EmployeeE1, ManagerM1 })
        {
            yield return Case("B09-15", user, HttpMethod.Get, One(DocumentAccessSeed.DocNoOwner), HttpStatusCode.Forbidden);
            yield return Case("B09-15", user, HttpMethod.Get, One(DocumentAccessSeed.DocDeletedLink), HttpStatusCode.Forbidden);
        }

        // B09-16: document admins read documents with no owner.
        foreach (var user in new[] { OfficerUnlinked, admin })
        {
            yield return Case("B09-16", user, HttpMethod.Get, One(DocumentAccessSeed.DocNoOwner), HttpStatusCode.OK);
            yield return Case("B09-16", user, HttpMethod.Get, FileOf(DocumentAccessSeed.DocNoOwner), HttpStatusCode.OK);
        }

        // B09-17: document admins (linked or not, and admins) read everything.
        foreach (var user in new[] { OfficerE2, OfficerUnlinked, admin })
        {
            yield return Case("B09-17", user, HttpMethod.Get, List, HttpStatusCode.OK);
            yield return Case("B09-17", user, HttpMethod.Get, ByEmployee(E1), HttpStatusCode.OK);
            yield return Case("B09-17", user, HttpMethod.Get, One(DocumentAccessSeed.DocE2Contract), HttpStatusCode.OK);
            yield return Case("B09-17", user, HttpMethod.Get, FileOf(DocumentAccessSeed.DocE2Contract), HttpStatusCode.OK);
        }

        // B09-20: paging limits.
        yield return Case("B09-20", admin, HttpMethod.Get, $"{List}?pageSize=101", HttpStatusCode.BadRequest);
        yield return Case("B09-20", admin, HttpMethod.Get, $"{List}?pageSize=0", HttpStatusCode.BadRequest);
        yield return Case("B09-20", admin, HttpMethod.Get, $"{List}?page=0", HttpStatusCode.BadRequest);

        // B09-21: not signed in.
        var anonymous = TestUsers.Anonymous;
        yield return Case("B09-21", anonymous, HttpMethod.Get, List, HttpStatusCode.Unauthorized);
        yield return Case("B09-21", anonymous, HttpMethod.Get, One(DocumentAccessSeed.DocE1Contract), HttpStatusCode.Unauthorized);
        yield return Case("B09-21", anonymous, HttpMethod.Get, FileOf(DocumentAccessSeed.DocE1Contract), HttpStatusCode.Unauthorized);
        yield return Case("B09-21", anonymous, HttpMethod.Post, List, HttpStatusCode.Unauthorized);
        yield return Case("B09-21", anonymous, HttpMethod.Put, One(DocumentAccessSeed.DocE1Contract), HttpStatusCode.Unauthorized, update);
        yield return Case("B09-21", anonymous, HttpMethod.Delete, One(DocumentAccessSeed.DocE1Contract), HttpStatusCode.Unauthorized);
        yield return Case("B09-21", anonymous, HttpMethod.Get, Types, HttpStatusCode.Unauthorized);

        // B09-22: document types stay open to every signed-in user.
        yield return Case("B09-22", EmployeeE1, HttpMethod.Get, Types, HttpStatusCode.OK);
    }

    [TestCaseSource(nameof(StatusCases))]
    public async Task Request_ReturnsExpectedStatus(
        TestUser user, HttpMethod method, string path, HttpStatusCode expected, object? body)
    {
        await ExpectStatusAsync(user, method, path, expected, body);
    }

    [Test]
    public async Task B09_02_EmployeeChangesAnotherEmployeesDocument_IsRefusedAndNothingChanges()
    {
        var user = EmployeeE1;
        var denyUpdate = DocumentAccessSeed.DocE2DeniedUpdate;
        var denyDelete = DocumentAccessSeed.DocE2DeniedDelete;

        await ExpectStatusAsync(user, HttpMethod.Put, One(denyUpdate), HttpStatusCode.Forbidden, ValidUpdate(E1, "B09-02 changed"));
        await ExpectStatusAsync(user, HttpMethod.Delete, One(denyDelete), HttpStatusCode.Forbidden);

        var rows = await Factory.QueryDatabaseAsync(db => db.Documents.AsNoTracking()
            .Where(d => d.Id == denyUpdate || d.Id == denyDelete)
            .Select(d => new
            {
                d.Id,
                d.Name,
                Owner = d.EmployeeDocuments.Where(ed => ed.IsActive && !ed.IsDeleted).Select(ed => (int?)ed.EmployeeId).FirstOrDefault(),
            })
            .ToListAsync());

        Assert.Multiple(() =>
        {
            Assert.That(rows.Select(r => r.Id), Is.EquivalentTo(new[] { denyUpdate, denyDelete }), "Both rows must still exist.");
            foreach (var row in rows)
            {
                Assert.That(row.Name, Is.EqualTo($"B09 document {row.Id}"), $"Document {row.Id} name must be unchanged.");
                Assert.That(row.Owner, Is.EqualTo(E2), $"Document {row.Id} must still belong to E2.");
            }

            Assert.That(File.Exists(PrivatePath(DocumentAccessSeed.StorageKeyOf(denyDelete, E2))), Is.True,
                "The refused delete must leave the file on disk.");
        });
    }

    [Test]
    public async Task B09_03_UploadByNonDocumentAdmin_IsRefusedAndWritesNothing()
    {
        foreach (var user in new[] { EmployeeE1, ManagerM1, DirectoryEditor, TestUsers.NoRole })
        {
            var rowsBefore = await DocumentCountAsync();
            var filesBefore = CountPrivateFiles();

            var (status, body) = await UploadAsync(user, E1, $"b09-03 {user.Description}.pdf", "B09-03 content");

            var rowsAfter = await DocumentCountAsync();
            var filesAfter = CountPrivateFiles();
            Assert.Multiple(() =>
            {
                Assert.That(status, Is.EqualTo(HttpStatusCode.Forbidden), $"{user.Description} -> POST {List}: body starts {Preview(body)}");
                Assert.That(rowsAfter, Is.EqualTo(rowsBefore), $"{user.Description}: no Document row may be created.");
                Assert.That(filesAfter, Is.EqualTo(filesBefore), $"{user.Description}: no file may be written under the private root.");
            });
        }
    }

    [Test]
    public async Task B09_04_SelfDownload_ReturnsTheSeededFile()
    {
        var user = EmployeeE1;
        var path = FileOf(DocumentAccessSeed.DocE1Contract);
        var body = await ExpectStatusAsync(user, HttpMethod.Get, path, HttpStatusCode.OK);
        Assert.That(body, Is.EqualTo(DocumentAccessSeed.ContentOf(DocumentAccessSeed.DocE1Contract)),
            BodyMessage(user, HttpMethod.Get, path, "file content", "the seeded content", body));
    }

    [Test]
    public async Task B09_14_DocumentAdmin_UnknownEmployee_GetsAnEmptyPage()
    {
        var user = TestUsers.Admin;
        var path = ByEmployee(MissingId);
        var json = await GetJsonAsync(user, path);

        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("totalCount").GetInt32(), Is.Zero, BodyMessage(user, HttpMethod.Get, path, "totalCount", "0", json.ToString()));
            Assert.That(json.GetProperty("items").GetArrayLength(), Is.Zero);
        });
    }

    [Test]
    public async Task B09_18_DocumentOfficerUpload_IsCreatedInThePrivateRoot()
    {
        var user = OfficerUnlinked;
        var (status, body) = await UploadAsync(user, E1, "b09-18 contract.pdf", "B09-18 content");
        Assert.That(status, Is.EqualTo(HttpStatusCode.Created), $"{user.Description} -> POST {List}: body starts {Preview(body)}");

        var json = ParseJson(body, user, HttpMethod.Post, List);
        var id = json.GetProperty("id").GetInt32();
        var storedKey = await Factory.QueryDatabaseAsync(db => db.Documents.AsNoTracking()
            .Where(d => d.Id == id).Select(d => d.StoredRelativePath).SingleAsync());

        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("employeeId").GetInt32(), Is.EqualTo(E1));
            Assert.That(json.GetProperty("employeeDisplayName").GetString(), Is.EqualTo("Eli Report"));
            Assert.That(File.Exists(PrivatePath(storedKey)), Is.True, "The file must be in the private root.");
        });
    }

    [Test]
    public async Task B09_19_Admin_UpdatesAndDeletesDocuments()
    {
        var user = TestUsers.Admin;
        var updated = await ExpectStatusAsync(user, HttpMethod.Put, One(DocumentAccessSeed.DocE2AdminUpdate), HttpStatusCode.OK,
            ValidUpdate(E1, "B09-19 moved to E1"));
        await ExpectStatusAsync(user, HttpMethod.Delete, One(DocumentAccessSeed.DocE2AdminDelete), HttpStatusCode.NoContent);

        var json = ParseJson(updated, user, HttpMethod.Put, One(DocumentAccessSeed.DocE2AdminUpdate));
        var deletedStillThere = await Factory.QueryDatabaseAsync(db => db.Documents.AsNoTracking()
            .AnyAsync(d => d.Id == DocumentAccessSeed.DocE2AdminDelete));

        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("name").GetString(), Is.EqualTo("B09-19 moved to E1"));
            Assert.That(json.GetProperty("employeeId").GetInt32(), Is.EqualTo(E1), "Update may move the document to another employee.");
            Assert.That(deletedStillThere, Is.False, "The deleted row must be gone.");
            Assert.That(File.Exists(PrivatePath(DocumentAccessSeed.StorageKeyOf(DocumentAccessSeed.DocE2AdminDelete, E2))), Is.False,
                "The deleted document's file must be removed.");
        });
    }

    [Test]
    public async Task B09_19b_Admin_EditKeepingTheSameOwner_KeepsOneLink()
    {
        // Review M1: the profile Edit modal always sends the current owner. InMemory does not enforce the unique
        // (EmployeeId, DocumentId) index, so the link rows are counted directly.
        var user = TestUsers.Admin;
        var id = DocumentAccessSeed.DocE2SameOwnerEdit;

        await ExpectStatusAsync(user, HttpMethod.Put, One(id), HttpStatusCode.OK, ValidUpdate(E2, "B09-19b first edit"));
        var body = await ExpectStatusAsync(user, HttpMethod.Put, One(id), HttpStatusCode.OK, ValidUpdate(E2, "B09-19b second edit"));

        var json = ParseJson(body, user, HttpMethod.Put, One(id));
        var links = await Factory.QueryDatabaseAsync(db => db.EmployeeDocuments.AsNoTracking()
            .Where(ed => ed.DocumentId == id)
            .Select(ed => new { ed.EmployeeId, ed.IsActive, ed.IsDeleted })
            .ToListAsync());

        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("name").GetString(), Is.EqualTo("B09-19b second edit"));
            Assert.That(json.GetProperty("employeeId").GetInt32(), Is.EqualTo(E2));
            Assert.That(links, Has.Count.EqualTo(1), "Editing with the same owner must not add a second link.");
            Assert.That(links[0].EmployeeId, Is.EqualTo(E2));
            Assert.That(links[0].IsActive && !links[0].IsDeleted, Is.True);
        });
    }

    [Test]
    public async Task B09_30_31_PagesAreOrderedAndDoNotOverlap()
    {
        var user = TestUsers.Admin;
        var first = await GetJsonAsync(user, $"{ByEmployee(P1)}&pageSize=25");
        var second = await GetJsonAsync(user, $"{ByEmployee(P1)}&pageSize=25&page=2");

        var firstIds = Ids(first);
        var secondIds = Ids(second);
        var firstTimes = first.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("updatedAtUtc").GetDateTime())
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(firstIds, Has.Count.EqualTo(25), "Page 1 should have 25 items.");
            Assert.That(first.GetProperty("totalCount").GetInt32(), Is.EqualTo(30));
            Assert.That(first.GetProperty("totalPages").GetInt32(), Is.EqualTo(2));
            Assert.That(first.GetProperty("page").GetInt32(), Is.EqualTo(1));
            Assert.That(firstTimes, Is.Ordered.Descending, "Items should be newest change first.");
            Assert.That(firstIds[0], Is.EqualTo(DocumentAccessSeed.FirstPagingDoc + DocumentAccessSeed.PagingDocCount - 1),
                "The newest paging document comes first.");
            Assert.That(secondIds, Has.Count.EqualTo(5), "Page 2 should have the last 5 items.");
            Assert.That(firstIds.Intersect(secondIds), Is.Empty, "No document may appear on both pages.");
            Assert.That(firstIds.Concat(secondIds), Is.EquivalentTo(
                Enumerable.Range(DocumentAccessSeed.FirstPagingDoc, DocumentAccessSeed.PagingDocCount)));
        });
    }

    [Test]
    public async Task B09_32_PagePastTheEnd_IsEmptyWithTheRealCount()
    {
        var json = await GetJsonAsync(TestUsers.Admin, $"{ByEmployee(P1)}&page=5");

        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("items").GetArrayLength(), Is.Zero);
            Assert.That(json.GetProperty("totalCount").GetInt32(), Is.EqualTo(30));
        });
    }

    [Test]
    public async Task B09_33_NoPagingParameters_UseTheDefaults()
    {
        var json = await GetJsonAsync(TestUsers.Admin, ByEmployee(P1));

        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("page").GetInt32(), Is.EqualTo(1));
            Assert.That(json.GetProperty("pageSize").GetInt32(), Is.EqualTo(25));
            Assert.That(json.GetProperty("items").GetArrayLength(), Is.EqualTo(25));
        });
    }

    [Test]
    public async Task B09_34_SelfList_HasOnlyOwnActiveDocuments_AndAScopedCount()
    {
        var user = EmployeeE1;
        var json = await GetJsonAsync(user, $"{ByEmployee(E1)}&pageSize=100");
        var ids = Ids(json);

        // E1's own documents: the seeded 8701 and 8702, plus any created for E1 by this fixture's own cases.
        var expected = await Factory.QueryDatabaseAsync(db => db.EmployeeDocuments.AsNoTracking()
            .Where(ed => ed.EmployeeId == E1 && ed.IsActive && !ed.IsDeleted)
            .Select(ed => ed.DocumentId)
            .ToListAsync());

        var oneItem = await GetJsonAsync(user, $"{ByEmployee(E1)}&pageSize=1");

        Assert.Multiple(() =>
        {
            Assert.That(ids, Does.Contain(DocumentAccessSeed.DocE1Contract));
            Assert.That(ids, Does.Contain(DocumentAccessSeed.DocE1Passport));
            Assert.That(ids, Does.Not.Contain(DocumentAccessSeed.DocDeletedLink), "A deleted link is not ownership.");
            Assert.That(ids, Is.EquivalentTo(expected));
            Assert.That(oneItem.GetProperty("items").GetArrayLength(), Is.EqualTo(1));
            Assert.That(oneItem.GetProperty("totalCount").GetInt32(), Is.EqualTo(expected.Count), "AC9: the count is E1's only.");
        });
        AssertNoStoragePath(json);
    }

    [Test]
    public async Task B09_35_TypeFilter_StaysInsideTheScope()
    {
        var json = await GetJsonAsync(ManagerM1, $"{ByEmployee(E1)}&documentTypeId={DocumentAccessSeed.TypePassport}");

        Assert.That(Ids(json), Is.EqualTo(new[] { DocumentAccessSeed.DocE1Passport }));
        await ExpectStatusAsync(ManagerM1, HttpMethod.Get, $"{List}?documentTypeId={DocumentAccessSeed.TypePassport}", HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task B09_36_37_ListAll_IncludesUnownedDocuments_WithDisplayNamesAndNoStoragePath()
    {
        var json = await GetJsonAsync(TestUsers.Admin, $"{List}?pageSize=100");
        var items = json.GetProperty("items").EnumerateArray().ToDictionary(i => i.GetProperty("id").GetInt32());

        Assert.Multiple(() =>
        {
            Assert.That(items.ContainsKey(DocumentAccessSeed.DocNoOwner), Is.True, "List all includes documents with no owner.");
            Assert.That(items.ContainsKey(DocumentAccessSeed.DocDeletedLink), Is.True, "List all includes documents with only a deleted link.");
            Assert.That(items[DocumentAccessSeed.DocNoOwner].GetProperty("employeeDisplayName").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(items[DocumentAccessSeed.DocNoOwner].GetProperty("employeeId").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(items[DocumentAccessSeed.DocDeletedLink].GetProperty("employeeDisplayName").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(items[DocumentAccessSeed.DocE1Contract].GetProperty("employeeDisplayName").GetString(), Is.EqualTo("Eli Report"));
            Assert.That(items[DocumentAccessSeed.DocE1Contract].GetProperty("documentTypeName").GetString(), Is.EqualTo("Contract"));
        });
        AssertNoStoragePath(json);
    }

    [Test]
    public async Task B09_40_Seed_AddsTopLevelDocumentsAdminMenu_GrantedToAdmin()
    {
        var menu = await Factory.QueryDatabaseAsync(db => db.Menus.AsNoTracking()
            .Where(m => m.Key == "documents.admin")
            .Select(m => new { m.Id, m.ParentMenuId, m.RoutePath, m.Label })
            .SingleOrDefaultAsync());
        Assert.That(menu, Is.Not.Null, "The documents.admin menu must be seeded.");

        var grantedToAdmin = await Factory.QueryDatabaseAsync(db => db.RoleKeyPermissions.AsNoTracking()
            .AnyAsync(rp => rp.RoleKey == TestSeed.AdminRoleKey && rp.MenuId == menu!.Id && rp.Allowed));

        Assert.Multiple(() =>
        {
            Assert.That(menu!.ParentMenuId, Is.Null, "documents.admin is a top-level menu.");
            Assert.That(menu.RoutePath, Is.EqualTo("/documents"));
            Assert.That(menu.Label, Is.EqualTo("Documents"));
            Assert.That(grantedToAdmin, Is.True, "ADMIN must hold documents.admin.");
        });
    }

    private static IEnumerable<TestCaseData> EligibilityCases()
    {
        yield return new TestCaseData(TestUsers.Admin, true).SetName("B09-41 Admin canManageAllDocuments is true");
        yield return new TestCaseData(OfficerUnlinked, true).SetName("B09-41 Document officer unlinked canManageAllDocuments is true");
        yield return new TestCaseData(EmployeeE1, false).SetName("B09-41 Employee E1 canManageAllDocuments is false");
        yield return new TestCaseData(ManagerM1, false).SetName("B09-41 Manager M1 canManageAllDocuments is false");
        yield return new TestCaseData(DirectoryEditor, false).SetName("B09-41 Directory editor canManageAllDocuments is false");
    }

    [TestCaseSource(nameof(EligibilityCases))]
    public async Task B09_41_EligibilityFlag_IsTrueOnlyForDocumentAdmins(TestUser user, bool expected)
    {
        var body = await ExpectStatusAsync(user, HttpMethod.Get, Eligibility, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, Eligibility);

        bool? actual = json.TryGetProperty("canManageAllDocuments", out var flag)
                       && flag.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? flag.GetBoolean()
            : null;

        Assert.That(actual, Is.EqualTo(expected),
            BodyMessage(user, HttpMethod.Get, Eligibility, "canManageAllDocuments", expected.ToString(), body));
    }

    private static void AssertNoStoragePath(JsonElement page)
    {
        foreach (var item in page.GetProperty("items").EnumerateArray())
        {
            var id = item.GetProperty("id").GetInt32();
            Assert.Multiple(() =>
            {
                Assert.That(item.TryGetProperty("downloadUrl", out var url) ? url.GetString() : null, Is.EqualTo($"/api/Documents/{id}/file"));
                Assert.That(item.TryGetProperty("storedRelativePath", out _), Is.False, "storedRelativePath must not be returned.");
            });
        }
    }

    private async Task<JsonElement> GetJsonAsync(TestUser user, string path)
    {
        var body = await ExpectStatusAsync(user, HttpMethod.Get, path, HttpStatusCode.OK);
        return ParseJson(body, user, HttpMethod.Get, path);
    }

    private async Task<(HttpStatusCode Status, string Body)> UploadAsync(TestUser user, int employeeId, string fileName, string content)
    {
        using var client = Factory.CreateClientAs(user);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(employeeId.ToString(CultureInfo.InvariantCulture)), "employeeId");
        form.Add(new StringContent(DocumentAccessSeed.TypeContract.ToString(CultureInfo.InvariantCulture)), "documentTypeId");
        form.Add(new StringContent("B09 upload"), "name");
        // B10: a real minimal PDF (text sent as application/pdf is now refused by the upload checker).
        var file = new ByteArrayContent(UploadTestFiles.Pdf(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", fileName);

        using var response = await client.PostAsync(List, form);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private Task<int> DocumentCountAsync()
        => Factory.QueryDatabaseAsync(db => db.Documents.AsNoTracking().CountAsync());

    private int CountPrivateFiles()
        => Directory.Exists(Factory.PrivateRootPath)
            ? Directory.GetFiles(Factory.PrivateRootPath, "*", SearchOption.AllDirectories).Length
            : 0;

    private string PrivatePath(string key)
        => Path.Combine(Factory.PrivateRootPath, key.Replace('/', Path.DirectorySeparatorChar));

    private static List<int> Ids(JsonElement page)
        => page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToList();

    private static UpdateDocumentRequestModel ValidUpdate(int employeeId, string name = "B09 denied change") => new()
    {
        Name = name,
        DocumentTypeId = DocumentAccessSeed.TypeContract,
        EmployeeId = employeeId,
    };

    private static string ByEmployee(int employeeId) => $"{List}?employeeId={employeeId}";

    private static string One(int id) => $"{List}/{id}";

    private static string FileOf(int id) => $"{List}/{id}/file";

    private static string Preview(string body)
        => string.IsNullOrEmpty(body) ? "(empty)" : body.Length <= 400 ? body : body[..400] + "...";
}
