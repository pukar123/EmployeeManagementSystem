using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EMS.API.IntegrationTests.Harness;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// B08: employee documents and leave attachments are stored in the private root (outside the web root) and are
/// downloadable only through the signed-in, access-checked <c>GET .../{id}/file</c> endpoints. Static files serve
/// only <c>/attachments/Organization</c> (public logos). Responses carry a <c>downloadUrl</c>, never a storage path.
/// Unsafe stored paths are treated as not found and never touch a file outside the private root.
/// This fixture has its own database and its own temporary web and private roots (from <see cref="EmsApiFactory"/>).
/// Each case that changes data uploads or seeds its own rows.
/// </summary>
[TestFixture]
public sealed class FileStorageTests : AccessTestFixture
{
    private const int DocumentTypeId = 8801;
    private const int LeaveTypeId = 8801;
    private const int RequestE1 = 8801;
    private const int SeededAttachmentE1 = 8801;
    private const int AnonymousDocumentId = 8801;
    private const int MissingFileDocumentId = 8802;
    private const string SeededAttachmentContent = "B08 seeded leave evidence";

    private static readonly int E1 = TestSeed.E1.EmployeeId;

    /// <summary>Unsafe stored paths (B08-11), each on its own document row.</summary>
    private static readonly (int Id, string Path)[] UnsafeDocuments =
    [
        (8811, "../../appsettings.json"),
        (8812, "Leave/../../x"),
        (8813, "/etc/passwd"),
        (8814, "Organization/1/logo.png"),
        (8815, "Leave/../../private-evil/Leave/a.txt"),
    ];

    private static TestUser EmployeeE1 => TestUsers.Employee(TestSeed.E1);
    private static TestUser EmployeeE2 => TestUsers.Employee(TestSeed.E2);

    [OneTimeSetUp]
    public async Task AddRowsAsync()
    {
        var now = DateTime.UtcNow;
        var attachmentKey = $"Leave/{RequestE1}/b08-seeded.txt";
        WriteFile(PrivatePath(attachmentKey), SeededAttachmentContent);

        await Factory.ChangeDatabaseAsync(db =>
        {
            db.DocumentTypes.Add(new DocumentType { Id = DocumentTypeId, Name = "B08 Contract", IsActive = true });

            db.Documents.Add(NewDocument(AnonymousDocumentId, $"Employee/{E1}/Documents/b08-anon.pdf", now));
            WriteFile(PrivatePath($"Employee/{E1}/Documents/b08-anon.pdf"), "B08 anonymous check");

            db.Documents.Add(NewDocument(MissingFileDocumentId, $"Employee/{E1}/Documents/b08-missing.pdf", now));

            foreach (var (id, path) in UnsafeDocuments)
                db.Documents.Add(NewDocument(id, path, now));

            db.LeaveTypes.Add(new LeaveType
            {
                Id = LeaveTypeId,
                OrganizationId = TestSeed.O1.Id,
                Name = "B08 Personal Leave",
                Unit = LeaveUnit.Days,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });

            db.LeaveRequests.Add(new LeaveRequest
            {
                Id = RequestE1,
                OrganizationId = TestSeed.O1.Id,
                EmployeeId = E1,
                LeaveTypeId = LeaveTypeId,
                StartDateUtc = new DateTime(2027, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDateUtc = new DateTime(2027, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                Unit = LeaveUnit.Days,
                RequestedAmount = 1m,
                Reason = "B08 seed",
                Status = LeaveRequestStatus.Pending,
                SubmittedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });

            db.LeaveRequestAttachments.Add(new LeaveRequestAttachment
            {
                Id = SeededAttachmentE1,
                LeaveRequestId = RequestE1,
                FileName = "b08 evidence.txt",
                ContentType = "text/plain",
                FileSizeBytes = Encoding.UTF8.GetByteCount(SeededAttachmentContent),
                StoragePath = attachmentKey,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });

            return Task.CompletedTask;
        });
    }

    [Test]
    public void Harness_UsesTheFactorysTemporaryWebRoot()
    {
        var env = Factory.Services.GetRequiredService<IWebHostEnvironment>();
        Assert.That(Path.GetFullPath(env.WebRootPath), Is.EqualTo(Path.GetFullPath(Factory.WebRootPath)),
            "The test host must use the factory's temporary web root, never EMS.API/wwwroot.");
    }

    [Test]
    public async Task B08_01_OldPublicLeaveUrl_Returns404()
    {
        WriteFile(WebPath("attachments/Leave/8801/x.txt"), "planted");
        await ExpectRawStatusAsync(TestUsers.Anonymous, "/attachments/Leave/8801/x.txt", HttpStatusCode.NotFound);
    }

    [Test]
    public async Task B08_02_OldPublicEmployeeUrl_Returns404ForAnonymousAndAdmin()
    {
        WriteFile(WebPath("attachments/Employee/5/Documents/x.pdf"), "planted");
        await ExpectRawStatusAsync(TestUsers.Anonymous, "/attachments/Employee/5/Documents/x.pdf", HttpStatusCode.NotFound);
        await ExpectRawStatusAsync(TestUsers.Admin, "/attachments/Employee/5/Documents/x.pdf", HttpStatusCode.NotFound);
    }

    [Test]
    public async Task B08_03_OtherWebRootFiles_Return404()
    {
        WriteFile(WebPath("attachments/Department/x.txt"), "planted");
        WriteFile(WebPath(".gitkeep"), string.Empty);
        WriteFile(WebPath("uploads/x"), "planted");
        await ExpectRawStatusAsync(TestUsers.Anonymous, "/attachments/Department/x.txt", HttpStatusCode.NotFound);
        await ExpectRawStatusAsync(TestUsers.Anonymous, "/.gitkeep", HttpStatusCode.NotFound);
        await ExpectRawStatusAsync(TestUsers.Anonymous, "/uploads/x", HttpStatusCode.NotFound);
    }

    [Test]
    public async Task B08_04_OrganisationLogo_StaysPublic()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var path = WebPath("attachments/Organization/1/logo-x.png");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, png);

        using var response = await ExpectRawStatusAsync(TestUsers.Anonymous, "/attachments/Organization/1/logo-x.png", HttpStatusCode.OK);
        var body = await response.Content.ReadAsByteArrayAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("image/png"));
            Assert.That(body, Is.EqualTo(png));
        });
    }

    [Test]
    public async Task B08_05_DocumentUpload_IsStoredPrivatelyAndReturnsDownloadUrl()
    {
        var webFilesBefore = CountFiles(Factory.WebRootPath);

        var (id, json) = await UploadDocumentAsync("b08-05 contract.pdf", UploadTestFiles.Pdf("B08-05 content"));

        var folder = Path.Combine(Factory.PrivateRootPath, "Employee", E1.ToString(CultureInfo.InvariantCulture), "Documents");
        var storedKey = await StoredDocumentPathAsync(id);

        Assert.Multiple(() =>
        {
            Assert.That(json.TryGetProperty("downloadUrl", out var url) ? url.GetString() : null,
                Is.EqualTo($"/api/Documents/{id}/file"));
            Assert.That(json.TryGetProperty("storedRelativePath", out _), Is.False, "storedRelativePath must not be returned.");
            Assert.That(storedKey, Does.StartWith($"Employee/{E1}/Documents/"), "The row must hold the storage key.");
            Assert.That(storedKey, Does.Not.StartWith("/"));
            Assert.That(File.Exists(PrivatePath(storedKey)), Is.True, "The file must be in the private root.");
            Assert.That(Directory.GetFiles(folder), Has.Length.GreaterThanOrEqualTo(1));
            Assert.That(CountFiles(Factory.WebRootPath), Is.EqualTo(webFilesBefore), "Nothing may be written under the web root.");
        });
    }

    [Test]
    public async Task B08_06_DocumentDownload_ReturnsFileWithHeaders()
    {
        const string fileName = "b08-06 contract.pdf";
        var content = UploadTestFiles.Pdf("B08-06 content");
        var (_, json) = await UploadDocumentAsync(fileName, content);
        var downloadUrl = json.GetProperty("downloadUrl").GetString()!;

        using var response = await ExpectRawStatusAsync(TestUsers.Admin, downloadUrl, HttpStatusCode.OK);
        await AssertFileResponseAsync(response, content, "application/pdf", fileName);
    }

    [Test]
    public async Task B08_07_DocumentDownload_WithoutToken_Returns401()
    {
        await ExpectRawStatusAsync(TestUsers.Anonymous, $"/api/Documents/{AnonymousDocumentId}/file", HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task B08_08_LeaveAttachmentUpload_IsStoredPrivatelyAndReturnsDownloadUrl()
    {
        var webFilesBefore = CountFiles(Factory.WebRootPath);
        // B10: a real PDF (text files are no longer accepted as leave attachments).
        var json = await UploadLeaveAttachmentAsync(EmployeeE1, RequestE1, "b08-08.pdf", UploadTestFiles.Pdf("B08-08 evidence"));
        var id = json.GetProperty("id").GetInt32();
        var storedKey = await Factory.QueryDatabaseAsync(db => db.LeaveRequestAttachments.AsNoTracking()
            .Where(a => a.Id == id).Select(a => a.StoragePath).SingleAsync());

        Assert.Multiple(() =>
        {
            Assert.That(json.TryGetProperty("downloadUrl", out var url) ? url.GetString() : null,
                Is.EqualTo($"/api/LeaveAttachments/{id}/file"));
            Assert.That(json.TryGetProperty("storagePath", out _), Is.False, "storagePath must not be returned.");
            Assert.That(storedKey, Does.StartWith($"Leave/{RequestE1}/"), "The row must hold the storage key.");
            Assert.That(File.Exists(PrivatePath(storedKey)), Is.True, "The file must be in the private root.");
            Assert.That(CountFiles(Factory.WebRootPath), Is.EqualTo(webFilesBefore), "Nothing may be written under the web root.");
        });
    }

    [Test]
    public async Task B08_09_LeaveAttachmentList_HasDownloadUrlAndNoStoragePath()
    {
        var path = $"/api/LeaveAttachments/request/{RequestE1}";
        var user = EmployeeE1;
        var body = await ExpectStatusAsync(user, HttpMethod.Get, path, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, path);

        Assert.That(json.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(json.GetArrayLength(), Is.GreaterThanOrEqualTo(1), BodyMessage(user, HttpMethod.Get, path, "items", "at least one", body));
        foreach (var item in json.EnumerateArray())
        {
            var id = item.GetProperty("id").GetInt32();
            Assert.Multiple(() =>
            {
                Assert.That(item.TryGetProperty("downloadUrl", out var url) ? url.GetString() : null,
                    Is.EqualTo($"/api/LeaveAttachments/{id}/file"));
                Assert.That(item.TryGetProperty("storagePath", out _), Is.False, "storagePath must not be returned.");
            });
        }
    }

    [Test]
    public async Task B08_10_LeaveAttachmentDownload_FollowsB06Access()
    {
        var path = $"/api/LeaveAttachments/{SeededAttachmentE1}/file";

        // B10: the seeded row is a legacy text/plain attachment. It still downloads, but as application/octet-stream
        // (text/plain is not on the download allow-list) with nosniff, so the browser never renders it.
        using (var own = await ExpectRawStatusAsync(EmployeeE1, path, HttpStatusCode.OK))
            await AssertFileResponseAsync(own, Encoding.UTF8.GetBytes(SeededAttachmentContent), "application/octet-stream", "b08 evidence.txt");

        using (var admin = await ExpectRawStatusAsync(TestUsers.Admin, path, HttpStatusCode.OK))
            Assert.That(await admin.Content.ReadAsStringAsync(), Is.EqualTo(SeededAttachmentContent));

        using var other = await ExpectRawStatusAsync(EmployeeE2, path, HttpStatusCode.Forbidden);
        Assert.That(await other.Content.ReadAsStringAsync(), Does.Not.Contain(SeededAttachmentContent));
    }

    [Test]
    public async Task B08_11_UnsafeStoredPaths_Return404AndDeleteTouchesNoFile()
    {
        var tempRoot = Path.GetDirectoryName(Factory.PrivateRootPath)!;
        var sentinels = new[]
        {
            Path.Combine(tempRoot, "x"),
            Path.Combine(tempRoot, "private-evil", "Leave", "a.txt"),
            PrivatePath("Organization/1/logo.png"),
            WebPath("attachments/Organization/1/logo.png"),
        };
        foreach (var sentinel in sentinels)
            WriteFile(sentinel, "sentinel");

        foreach (var (id, storedPath) in UnsafeDocuments)
        {
            using (await ExpectRawStatusAsync(TestUsers.Admin, $"/api/Documents/{id}/file", HttpStatusCode.NotFound, storedPath))
            {
            }

            await ExpectStatusAsync(TestUsers.Admin, HttpMethod.Delete, $"/api/Documents/{id}", HttpStatusCode.NoContent);
        }

        Assert.Multiple(() =>
        {
            foreach (var sentinel in sentinels)
                Assert.That(File.Exists(sentinel), Is.True, $"{sentinel} must not be deleted by an unsafe row.");
        });
    }

    [Test]
    public async Task B08_12_StoredFileMissing_Download404AndDelete204()
    {
        await ExpectRawStatusAsync(TestUsers.Admin, $"/api/Documents/{MissingFileDocumentId}/file", HttpStatusCode.NotFound);
        await ExpectStatusAsync(TestUsers.Admin, HttpMethod.Delete, $"/api/Documents/{MissingFileDocumentId}", HttpStatusCode.NoContent);
    }

    [Test]
    public async Task B08_13_DocumentDelete_RemovesPrivateFile()
    {
        var (id, _) = await UploadDocumentAsync("b08-13.pdf", UploadTestFiles.Pdf("B08-13 content"));
        var storedKey = await StoredDocumentPathAsync(id);
        Assert.That(File.Exists(PrivatePath(storedKey)), Is.True, "Precondition: the uploaded file exists.");

        await ExpectStatusAsync(TestUsers.Admin, HttpMethod.Delete, $"/api/Documents/{id}", HttpStatusCode.NoContent);

        Assert.That(File.Exists(PrivatePath(storedKey)), Is.False, "The file must be removed from the private root.");
    }

    private static Document NewDocument(int id, string storedPath, DateTime now) => new()
    {
        Id = id,
        DocumentTypeId = DocumentTypeId,
        Name = $"B08 document {id}",
        FileKind = DocumentFileKind.Pdf,
        OriginalFileName = $"b08-{id}.pdf",
        ContentType = "application/pdf",
        StoredRelativePath = storedPath,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    private async Task<(int Id, JsonElement Json)> UploadDocumentAsync(string fileName, byte[] content)
    {
        const string path = "/api/Documents";
        var user = TestUsers.Admin;
        using var client = Factory.CreateClientAs(user);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(E1.ToString(CultureInfo.InvariantCulture)), "employeeId");
        form.Add(new StringContent(DocumentTypeId.ToString(CultureInfo.InvariantCulture)), "documentTypeId");
        form.Add(new StringContent("B08 contract"), "name");
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", fileName);

        using var response = await client.PostAsync(path, form);
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
            $"{user.Description} -> POST {path}: expected 201, got {(int)response.StatusCode}. Body starts: {Preview(body)}");

        var json = ParseJson(body, user, HttpMethod.Post, path);
        return (json.GetProperty("id").GetInt32(), json);
    }

    private async Task<JsonElement> UploadLeaveAttachmentAsync(TestUser user, int leaveRequestId, string fileName, byte[] content)
    {
        const string path = "/api/LeaveAttachments";
        using var client = Factory.CreateClientAs(user);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(leaveRequestId.ToString(CultureInfo.InvariantCulture)), "leaveRequestId");
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", fileName);

        using var response = await client.PostAsync(path, form);
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            $"{user.Description} -> POST {path}: expected 200, got {(int)response.StatusCode}. Body starts: {Preview(body)}");

        return ParseJson(body, user, HttpMethod.Post, path);
    }

    /// <summary>Sends a GET and asserts the status. The caller disposes the response to read headers and body.</summary>
    private async Task<HttpResponseMessage> ExpectRawStatusAsync(TestUser user, string path, HttpStatusCode expected, string? note = null)
    {
        var client = Factory.CreateClientAs(user);
        var response = await client.GetAsync(path);
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"{user.Description} -> GET {path}{(note is null ? string.Empty : $" (stored path {note})")}: " +
                        $"expected {(int)expected} {expected}, got {(int)response.StatusCode} {response.StatusCode}. Body starts: {Preview(body)}");
        }

        return response;
    }

    private static async Task AssertFileResponseAsync(HttpResponseMessage response, byte[] expectedBody, string expectedType, string expectedName)
    {
        var body = await response.Content.ReadAsByteArrayAsync();
        var disposition = response.Content.Headers.ContentDisposition;

        Assert.Multiple(() =>
        {
            Assert.That(body, Is.EqualTo(expectedBody));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(expectedType));
            Assert.That(disposition?.DispositionType, Is.EqualTo("attachment"));
            Assert.That(disposition?.FileName?.Trim('"'), Is.EqualTo(expectedName), "filename");
            Assert.That(disposition?.FileNameStar, Is.EqualTo(expectedName), "filename*");
            Assert.That(response.Headers.TryGetValues("X-Content-Type-Options", out var noSniff) ? string.Join(",", noSniff) : null,
                Is.EqualTo("nosniff"), "X-Content-Type-Options");
        });
    }

    private Task<string> StoredDocumentPathAsync(int id)
        => Factory.QueryDatabaseAsync(db => db.Documents.AsNoTracking()
            .Where(d => d.Id == id).Select(d => d.StoredRelativePath).SingleAsync());

    private string PrivatePath(string key)
        => Path.Combine(Factory.PrivateRootPath, key.Replace('/', Path.DirectorySeparatorChar));

    private string WebPath(string relative)
        => Path.Combine(Factory.WebRootPath, relative.Replace('/', Path.DirectorySeparatorChar));

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static int CountFiles(string root)
        => Directory.Exists(root) ? Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length : 0;

    private static string Preview(string body)
        => string.IsNullOrEmpty(body) ? "(empty)" : body.Length <= 400 ? body : body[..400] + "...";
}
