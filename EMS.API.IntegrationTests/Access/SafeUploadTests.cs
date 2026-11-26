using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EMS.API.IntegrationTests.Harness;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// B10: safe upload rules on every upload endpoint (I01 to I20). Each upload is checked by the shared upload checker
/// (extension, declared content type and content signature must match one allowed type), stored under a
/// server-generated key with the server's extension, and downloaded as an attachment with a sanitised name, an
/// allow-listed content type and <c>X-Content-Type-Options: nosniff</c>. Access checks (B06, B09) still come first.
/// This fixture has its own database and temporary roots; each case uploads or seeds its own rows.
/// </summary>
[TestFixture]
public sealed class SafeUploadTests : AccessTestFixture
{
    private const string Documents = "/api/Documents";
    private const string LeaveAttachments = "/api/LeaveAttachments";
    private const int DocumentTypeId = 9101;
    private const int LeaveTypeId = 9101;
    private const int RequestE1 = 9101;
    private const int RequestE2 = 9102;
    private const int LegacyTextDocumentId = 9101;
    private const int LegacyHtmlDocumentId = 9102;
    private const int LegacyTextAttachmentId = 9101;
    private const int LegacyHtmlAttachmentId = 9102;
    private const int OrgValidLogo = 9101;
    private const int OrgRefusedLogo = 9102;
    private const int OrgLargeLogo = 9103;
    private const string LegacyText = "B10 legacy text";
    private const string LegacyHtml = "<html><script>alert(1)</script></html>";
    private const string DocumentTypesMessage = "This file type is not allowed. Allowed types: PDF, Word (.docx), PNG, JPEG.";
    private const string LeaveTypesMessage = "This file type is not allowed. Allowed types: PDF, PNG, JPEG.";
    private const string LogoTypesMessage = "This file type is not allowed. Allowed types: PNG, JPEG, WebP.";
    private const long OneMegabyte = 1024 * 1024;

    private static readonly int E1 = TestSeed.E1.EmployeeId;
    private static readonly string DocumentKeyPattern = $"^Employee/{E1}/Documents/[0-9a-f]{{32}}\\.(pdf|docx|png|jpg)$";

    private static TestUser EmployeeE1 => TestUsers.Employee(TestSeed.E1);

    private static string OldLogoPath(int orgId) => $"/attachments/Organization/{orgId}/logo-old.png";

    [OneTimeSetUp]
    public async Task AddRowsAsync()
    {
        var now = DateTime.UtcNow;
        WriteFile(PrivatePath($"Employee/{E1}/Documents/b10-legacy.txt"), LegacyText);
        WriteFile(PrivatePath($"Employee/{E1}/Documents/b10-legacy.html"), LegacyHtml);
        WriteFile(PrivatePath($"Leave/{RequestE1}/b10-legacy.txt"), LegacyText);
        WriteFile(PrivatePath($"Leave/{RequestE1}/b10-legacy.html"), LegacyHtml);

        foreach (var orgId in new[] { OrgValidLogo, OrgRefusedLogo, OrgLargeLogo })
            await File.WriteAllBytesAsync(EnsureFolder(WebPath(OldLogoPath(orgId).TrimStart('/'))), UploadTestFiles.Png());

        await Factory.ChangeDatabaseAsync(db =>
        {
            db.DocumentTypes.Add(new DocumentType { Id = DocumentTypeId, Name = "B10 Contract", IsActive = true });

            db.Documents.Add(LegacyDocument(LegacyTextDocumentId, $"Employee/{E1}/Documents/b10-legacy.txt", "text/plain", "b10 legacy.txt", now));
            db.Documents.Add(LegacyDocument(LegacyHtmlDocumentId, $"Employee/{E1}/Documents/b10-legacy.html", "text/html", "a\"\r\nb.html", now));

            db.LeaveTypes.Add(new LeaveType
            {
                Id = LeaveTypeId,
                OrganizationId = TestSeed.O1.Id,
                Name = "B10 Personal Leave",
                Unit = LeaveUnit.Days,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            db.LeaveRequests.Add(NewLeaveRequest(RequestE1, E1, now));
            db.LeaveRequests.Add(NewLeaveRequest(RequestE2, TestSeed.E2.EmployeeId, now));

            db.LeaveRequestAttachments.Add(LegacyAttachment(LegacyTextAttachmentId, $"Leave/{RequestE1}/b10-legacy.txt", "text/plain", "b10 legacy.txt", LegacyText, now));
            db.LeaveRequestAttachments.Add(LegacyAttachment(LegacyHtmlAttachmentId, $"Leave/{RequestE1}/b10-legacy.html", "text/html", "a\"\r\nb.html", LegacyHtml, now));

            foreach (var orgId in new[] { OrgValidLogo, OrgRefusedLogo, OrgLargeLogo })
                db.Organizations.Add(new Organization { Id = orgId, Name = $"B10 Org {orgId}", IsActive = true, LogoRelativePath = OldLogoPath(orgId) });

            return Task.CompletedTask;
        });
    }

    // ---- Employee documents ----

    [Test]
    public async Task I01_ValidPdf_IsStoredWithAServerKeyAndCanonicalType()
    {
        var bytes = UploadTestFiles.Pdf("I01");
        var (status, body) = await UploadDocumentAsync(TestUsers.Admin, "b10 contract.PDF", "application/pdf", bytes);
        Assert.That(status, Is.EqualTo(HttpStatusCode.Created), Preview(body));

        var row = await DocumentRowAsync(Id(body));
        Assert.Multiple(() =>
        {
            Assert.That(row.StoredRelativePath, Does.Match(DocumentKeyPattern));
            Assert.That(row.StoredRelativePath, Does.EndWith(".pdf"));
            Assert.That(row.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(row.OriginalFileName, Is.EqualTo("b10 contract.pdf"));
            Assert.That(row.FileKind, Is.EqualTo(DocumentFileKind.Pdf));
            Assert.That(row.StoredRelativePath, Does.Not.Contain("contract"), "AC10: the user's name must not be in the key.");
            Assert.That(File.ReadAllBytes(PrivatePath(row.StoredRelativePath)), Is.EqualTo(bytes));
        });
    }

    [Test]
    public async Task I02_ValidDocx_IsStoredAsWord()
    {
        var (status, body) = await UploadDocumentAsync(TestUsers.Admin, "b10 letter.docx", UploadTestFiles.DocxType, UploadTestFiles.Docx());
        Assert.That(status, Is.EqualTo(HttpStatusCode.Created), Preview(body));

        var row = await DocumentRowAsync(Id(body));
        Assert.Multiple(() =>
        {
            Assert.That(row.StoredRelativePath, Does.Match(DocumentKeyPattern));
            Assert.That(row.StoredRelativePath, Does.EndWith(".docx"));
            Assert.That(row.ContentType, Is.EqualTo(UploadTestFiles.DocxType));
            Assert.That(row.FileKind, Is.EqualTo(DocumentFileKind.Word));
        });
    }

    [Test]
    public async Task I02b_ValidPngAndJpeg_AreStoredAsImages()
    {
        var (pngStatus, pngBody) = await UploadDocumentAsync(TestUsers.Admin, "scan.png", "image/png", UploadTestFiles.Png());
        var (jpegStatus, jpegBody) = await UploadDocumentAsync(TestUsers.Admin, "photo.JPEG", "image/jpeg", UploadTestFiles.Jpeg());
        Assert.That(pngStatus, Is.EqualTo(HttpStatusCode.Created), Preview(pngBody));
        Assert.That(jpegStatus, Is.EqualTo(HttpStatusCode.Created), Preview(jpegBody));

        var png = await DocumentRowAsync(Id(pngBody));
        var jpeg = await DocumentRowAsync(Id(jpegBody));
        Assert.Multiple(() =>
        {
            Assert.That(png.StoredRelativePath, Does.Match(DocumentKeyPattern).And.EndWith(".png"));
            Assert.That(png.FileKind, Is.EqualTo(DocumentFileKind.Image));
            Assert.That(jpeg.StoredRelativePath, Does.Match(DocumentKeyPattern).And.EndWith(".jpg"));
            Assert.That(jpeg.ContentType, Is.EqualTo("image/jpeg"));
            Assert.That(jpeg.OriginalFileName, Is.EqualTo("photo.jpg"));
        });
    }

    [TestCase("x.pdf", "application/pdf", TestName = "I03 HTML named x.pdf declared application/pdf is refused")]
    [TestCase("x.html", "application/pdf", TestName = "I04 HTML named x.html declared application/pdf is refused")]
    public async Task I03_I04_HtmlDisguisedAsPdf_IsRefusedAndWritesNothing(string name, string type)
        => await ExpectDocumentRefusedAsync(name, type, UploadTestFiles.Html(), HttpStatusCode.BadRequest, "file_type_not_allowed", DocumentTypesMessage);

    [TestCase("a.png", "image/png", "pdf", TestName = "AC4a PDF bytes named .png are refused")]
    [TestCase("a.png", "application/pdf", "png", TestName = "AC4b PNG bytes named .png declared application/pdf are refused")]
    [TestCase("a.pdf", "application/octet-stream", "pdf", TestName = "AC4c PDF declared application/octet-stream is refused")]
    [TestCase("a.doc", "application/msword", "pdf", TestName = "AC5 legacy .doc is refused")]
    public async Task AC4_MismatchedFile_IsRefusedAndWritesNothing(string name, string type, string bytesKind)
    {
        var bytes = bytesKind == "png" ? UploadTestFiles.Png() : UploadTestFiles.Pdf();
        await ExpectDocumentRefusedAsync(name, type, bytes, HttpStatusCode.BadRequest, "file_type_not_allowed", DocumentTypesMessage);
    }

    [Test]
    public async Task AC5_CorruptDocx_IsRefusedWith400NotA500()
    {
        byte[] broken = [0x50, 0x4B, 0x03, 0x04, .. Enumerable.Repeat((byte)0x41, 200)];
        await ExpectDocumentRefusedAsync("a.docx", UploadTestFiles.DocxType, broken, HttpStatusCode.BadRequest, "file_type_not_allowed", DocumentTypesMessage);
    }

    [Test]
    public async Task I05_EmptyFile_IsRefusedAsFileEmpty()
        => await ExpectDocumentRefusedAsync("empty.pdf", "application/pdf", [], HttpStatusCode.BadRequest, "file_empty", "The file is empty.");

    [Test]
    public async Task I05b_MissingFilePart_StillSaysAFileIsRequired()
    {
        using var client = Factory.CreateClientAs(TestUsers.Admin);
        using var form = DocumentForm();
        using var response = await client.PostAsync(Documents, form);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), Preview(body));
            Assert.That(Message(body), Is.EqualTo("A file is required."));
        });
    }

    [Test]
    public async Task I06_FileOverFifteenMegabytes_Is413FileTooLarge()
        => await ExpectDocumentRefusedAsync("big.pdf", "application/pdf",
            UploadTestFiles.Padded(UploadTestFiles.Pdf(), 15 * OneMegabyte + 1),
            HttpStatusCode.RequestEntityTooLarge, "file_too_large", "The file is too large. The maximum size is 15 MB.");

    [Test]
    public async Task I06b_FileOfExactlyFifteenMegabytes_IsAccepted()
    {
        var (status, body) = await UploadDocumentAsync(TestUsers.Admin, "exact.pdf", "application/pdf",
            UploadTestFiles.Padded(UploadTestFiles.Pdf(), 15 * OneMegabyte));
        Assert.That(status, Is.EqualTo(HttpStatusCode.Created), Preview(body));
    }

    [Test]
    public async Task I07_RequestOverTheSixteenMegabyteCap_Is413RequestTooLarge()
        => await ExpectDocumentRefusedAsync("huge.pdf", "application/pdf",
            UploadTestFiles.Padded(UploadTestFiles.Pdf(), 16 * OneMegabyte + 1),
            HttpStatusCode.RequestEntityTooLarge, "request_too_large", "The file is too large. The maximum size is 15 MB.");

    [Test]
    public async Task AC15_NonDocumentAdminUpload_IsStill403AndWritesNothing()
    {
        var filesBefore = CountFiles(Factory.PrivateRootPath);
        var (status, body) = await UploadDocumentAsync(EmployeeE1, "x.pdf", "application/pdf", UploadTestFiles.Html());
        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.Forbidden), Preview(body));
            Assert.That(CountFiles(Factory.PrivateRootPath), Is.EqualTo(filesBefore));
        });
    }

    [Test]
    public async Task I17_DocumentDownload_HasSafeHeaders()
    {
        var bytes = UploadTestFiles.Pdf("I17");
        var (status, body) = await UploadDocumentAsync(TestUsers.Admin, "b10 contract.PDF", "application/pdf", bytes);
        Assert.That(status, Is.EqualTo(HttpStatusCode.Created), Preview(body));

        using var response = await GetAsync(TestUsers.Admin, $"{Documents}/{Id(body)}/file", HttpStatusCode.OK);
        await AssertDownloadAsync(response, bytes, "application/pdf", "b10 contract.pdf");
    }

    [Test]
    public async Task I20_NameWithControlQuoteAndBidiCharacters_IsSanitisedWhenStoredAndDownloaded()
    {
        var bytes = UploadTestFiles.Pdf("I20");
        var (status, body) = await UploadDocumentAsync(TestUsers.Admin, "C:\\fakepath\\a\"b\u202E\r\n.PDF", "application/pdf", bytes,
            nameAsFileNameStar: true);
        Assert.That(status, Is.EqualTo(HttpStatusCode.Created), Preview(body));

        var id = Id(body);
        var row = await DocumentRowAsync(id);
        Assert.That(row.OriginalFileName, Is.EqualTo("a_b.pdf"));

        using var response = await GetAsync(TestUsers.Admin, $"{Documents}/{id}/file", HttpStatusCode.OK);
        await AssertDownloadAsync(response, bytes, "application/pdf", "a_b.pdf");
    }

    // ---- Leave attachments ----

    [Test]
    public async Task I08_ValidJpeg_IsStoredWithAServerKey()
    {
        var bytes = UploadTestFiles.Jpeg();
        var (status, body) = await UploadLeaveAsync(EmployeeE1, RequestE1, "medical certificate.JPG", "image/jpeg", bytes);
        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), Preview(body));

        var row = await AttachmentRowAsync(Id(body));
        Assert.Multiple(() =>
        {
            Assert.That(row.StoragePath, Does.Match($"^Leave/{RequestE1}/[0-9a-f]{{32}}\\.jpg$"));
            Assert.That(row.ContentType, Is.EqualTo("image/jpeg"));
            Assert.That(row.FileName, Is.EqualTo("medical certificate.jpg"));
            Assert.That(row.FileSizeBytes, Is.EqualTo(bytes.Length));
        });
    }

    [Test]
    public async Task I09_ExeRenamedPng_IsRefusedAndWritesNothing()
        => await ExpectLeaveRefusedAsync(EmployeeE1, RequestE1, "cert.png", "image/png", UploadTestFiles.Exe(),
            HttpStatusCode.BadRequest, "file_type_not_allowed", LeaveTypesMessage);

    [Test]
    public async Task I10_TextFile_IsRefused()
        => await ExpectLeaveRefusedAsync(EmployeeE1, RequestE1, "note.txt", "text/plain", "B10 note"u8.ToArray(),
            HttpStatusCode.BadRequest, "file_type_not_allowed", LeaveTypesMessage);

    [Test]
    public async Task AC6_Docx_IsRefusedForLeave()
        => await ExpectLeaveRefusedAsync(EmployeeE1, RequestE1, "a.docx", UploadTestFiles.DocxType, UploadTestFiles.Docx(),
            HttpStatusCode.BadRequest, "file_type_not_allowed", LeaveTypesMessage);

    [Test]
    public async Task AC8_EmptyLeaveFile_IsFileEmpty()
        => await ExpectLeaveRefusedAsync(EmployeeE1, RequestE1, "a.pdf", "application/pdf", [],
            HttpStatusCode.BadRequest, "file_empty", "The file is empty.");

    [Test]
    public async Task AC9_LeaveFileOverFifteenMegabytes_Is413()
        => await ExpectLeaveRefusedAsync(EmployeeE1, RequestE1, "big.pdf", "application/pdf",
            UploadTestFiles.Padded(UploadTestFiles.Pdf(), 15 * OneMegabyte + 1),
            HttpStatusCode.RequestEntityTooLarge, "file_too_large", "The file is too large. The maximum size is 15 MB.");

    [Test]
    public async Task I11_InvalidFileForAnotherEmployeesRequest_Is403First()
        => await ExpectLeaveRefusedAsync(EmployeeE1, RequestE2, "x.pdf", "application/pdf", UploadTestFiles.Html(),
            HttpStatusCode.Forbidden, null, null);

    [Test]
    public async Task I18_LeaveDownload_HasSafeHeaders()
    {
        var bytes = UploadTestFiles.Jpeg();
        var (status, body) = await UploadLeaveAsync(EmployeeE1, RequestE1, "b10 photo.jpeg", "image/jpeg", bytes);
        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), Preview(body));

        using var response = await GetAsync(EmployeeE1, $"{LeaveAttachments}/{Id(body)}/file", HttpStatusCode.OK);
        await AssertDownloadAsync(response, bytes, "image/jpeg", "b10 photo.jpg");
    }

    // ---- Legacy rows saved before B10 ----

    private static IEnumerable<TestCaseData> LegacyDownloads()
    {
        yield return new TestCaseData($"{Documents}/{LegacyTextDocumentId}/file", LegacyText, "b10 legacy.txt").SetName("I19a legacy text/plain document");
        yield return new TestCaseData($"{Documents}/{LegacyHtmlDocumentId}/file", LegacyHtml, "a_b.html").SetName("I19b legacy text/html document");
        yield return new TestCaseData($"{LeaveAttachments}/{LegacyTextAttachmentId}/file", LegacyText, "b10 legacy.txt").SetName("I19c legacy text/plain leave attachment");
        yield return new TestCaseData($"{LeaveAttachments}/{LegacyHtmlAttachmentId}/file", LegacyHtml, "a_b.html").SetName("I19d legacy text/html leave attachment");
    }

    [TestCaseSource(nameof(LegacyDownloads))]
    public async Task I19_LegacyRow_DownloadsAsOctetStreamAttachment(string path, string content, string expectedName)
    {
        using var response = await GetAsync(TestUsers.Admin, path, HttpStatusCode.OK);
        await AssertDownloadAsync(response, Encoding.UTF8.GetBytes(content), "application/octet-stream", expectedName);
    }

    // ---- Organisation logo ----

    [Test]
    public async Task I12_ValidPngLogo_IsSavedAndTheOldLogoIsRemoved()
    {
        var (status, body) = await UploadLogoAsync(OrgValidLogo, "Company Logo.PNG", "image/png", UploadTestFiles.Png());
        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), Preview(body));

        var path = await LogoPathAsync(OrgValidLogo);
        Assert.Multiple(() =>
        {
            Assert.That(path, Does.Match($"^/attachments/Organization/{OrgValidLogo}/logo-[0-9a-f]{{32}}\\.png$"));
            Assert.That(path, Does.Not.Contain("Company"), "AC10: the user's name must not be in the logo file name.");
            Assert.That(File.Exists(WebPath(path!.TrimStart('/'))), Is.True, "The new logo must be on disk.");
            Assert.That(File.Exists(WebPath(OldLogoPath(OrgValidLogo).TrimStart('/'))), Is.False,
                "The old logo is deleted after the database points to the new one.");
        });
    }

    [TestCase("logo.svg", "image/svg+xml", TestName = "I13a SVG logo is refused and the current logo is kept")]
    [TestCase("logo.png", "image/png", TestName = "I13b SVG text named .png is refused and the current logo is kept")]
    public async Task I13_SvgLogo_IsRefusedAndTheCurrentLogoIsUnchanged(string name, string type)
        => await ExpectLogoRefusedAsync(OrgRefusedLogo, name, type, UploadTestFiles.Svg(),
            HttpStatusCode.BadRequest, "file_type_not_allowed", LogoTypesMessage);

    [Test]
    public async Task AC8_EmptyLogo_IsFileEmptyAndTheCurrentLogoIsUnchanged()
        => await ExpectLogoRefusedAsync(OrgRefusedLogo, "logo.png", "image/png", [],
            HttpStatusCode.BadRequest, "file_empty", "The file is empty.");

    [Test]
    public async Task I14_LogoOverFiveMegabytes_Is413AndTheCurrentLogoIsUnchanged()
        => await ExpectLogoRefusedAsync(OrgLargeLogo, "logo.png", "image/png",
            UploadTestFiles.Padded(UploadTestFiles.Png(), 5 * OneMegabyte + 1),
            HttpStatusCode.RequestEntityTooLarge, "file_too_large", "The file is too large. The maximum size is 5 MB.");

    [Test]
    public async Task I15_PlantedPngLogo_IsServedInlineWithNoSniff()
    {
        var png = UploadTestFiles.Png();
        await File.WriteAllBytesAsync(EnsureFolder(WebPath("attachments/Organization/1/logo-b10.png")), png);

        using var response = await GetAsync(TestUsers.Anonymous, "/attachments/Organization/1/logo-b10.png", HttpStatusCode.OK);
        var body = await response.Content.ReadAsByteArrayAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body, Is.EqualTo(png));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("image/png"));
            Assert.That(NoSniff(response), Is.EqualTo("nosniff"));
            Assert.That(response.Content.Headers.ContentDisposition, Is.Null, "Logos are inline images.");
        });
    }

    [TestCase("logo-b10.svg")]
    [TestCase("logo-b10.html")]
    [TestCase("logo-b10.txt")]
    public async Task I16_PlantedNonImageInTheLogoFolder_Returns404(string fileName)
    {
        await File.WriteAllBytesAsync(EnsureFolder(WebPath($"attachments/Organization/1/{fileName}")), UploadTestFiles.Svg());
        using var _ = await GetAsync(TestUsers.Anonymous, $"/attachments/Organization/1/{fileName}", HttpStatusCode.NotFound);
    }

    // ---- Helpers ----

    private async Task ExpectDocumentRefusedAsync(string name, string type, byte[] bytes, HttpStatusCode expected, string code, string message)
    {
        var rowsBefore = await Factory.QueryDatabaseAsync(db => db.Documents.AsNoTracking().CountAsync());
        var filesBefore = CountFiles(Factory.PrivateRootPath);

        var (status, body) = await UploadDocumentAsync(TestUsers.Admin, name, type, bytes);

        var rowsAfter = await Factory.QueryDatabaseAsync(db => db.Documents.AsNoTracking().CountAsync());
        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(expected), $"POST {Documents} {name} ({type}): body starts {Preview(body)}");
            Assert.That(Code(body), Is.EqualTo(code), "code");
            Assert.That(Message(body), Is.EqualTo(message), "message");
            Assert.That(rowsAfter, Is.EqualTo(rowsBefore), "No document row may be created.");
            Assert.That(CountFiles(Factory.PrivateRootPath), Is.EqualTo(filesBefore), "No file may be written.");
        });
    }

    private async Task ExpectLeaveRefusedAsync(
        TestUser user, int requestId, string name, string type, byte[] bytes, HttpStatusCode expected, string? code, string? message)
    {
        var rowsBefore = await Factory.QueryDatabaseAsync(db => db.LeaveRequestAttachments.AsNoTracking().CountAsync());
        var filesBefore = CountFiles(Factory.PrivateRootPath);

        var (status, body) = await UploadLeaveAsync(user, requestId, name, type, bytes);

        var rowsAfter = await Factory.QueryDatabaseAsync(db => db.LeaveRequestAttachments.AsNoTracking().CountAsync());
        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(expected), $"{user.Description} -> POST {LeaveAttachments} {name} ({type}): body starts {Preview(body)}");
            if (code is not null)
            {
                Assert.That(Code(body), Is.EqualTo(code), "code");
                Assert.That(Message(body), Is.EqualTo(message), "message");
            }

            Assert.That(rowsAfter, Is.EqualTo(rowsBefore), "No attachment row may be created.");
            Assert.That(CountFiles(Factory.PrivateRootPath), Is.EqualTo(filesBefore), "No file may be written.");
        });
    }

    private async Task ExpectLogoRefusedAsync(int orgId, string name, string type, byte[] bytes, HttpStatusCode expected, string code, string message)
    {
        var logoFolder = Path.GetDirectoryName(WebPath(OldLogoPath(orgId).TrimStart('/')))!;
        var filesBefore = CountFiles(logoFolder);

        var (status, body) = await UploadLogoAsync(orgId, name, type, bytes);

        var path = await LogoPathAsync(orgId);
        Assert.Multiple(() =>
        {
            Assert.That(status, Is.EqualTo(expected), $"POST logo {name} ({type}): body starts {Preview(body)}");
            Assert.That(Code(body), Is.EqualTo(code), "code");
            Assert.That(Message(body), Is.EqualTo(message), "message");
            Assert.That(path, Is.EqualTo(OldLogoPath(orgId)), "AC16: logoRelativePath must be unchanged.");
            Assert.That(File.Exists(WebPath(OldLogoPath(orgId).TrimStart('/'))), Is.True, "AC16: the current logo file must be kept.");
            Assert.That(CountFiles(logoFolder), Is.EqualTo(filesBefore), "No new logo file may be left behind.");
        });
    }

    private async Task<(HttpStatusCode Status, string Body)> UploadDocumentAsync(
        TestUser user, string fileName, string contentType, byte[] bytes, bool nameAsFileNameStar = false)
    {
        using var client = Factory.CreateClientAs(user);
        using var form = DocumentForm();
        form.Add(FilePart(fileName, contentType, bytes, nameAsFileNameStar));
        using var response = await client.PostAsync(Documents, form);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private async Task<(HttpStatusCode Status, string Body)> UploadLeaveAsync(
        TestUser user, int requestId, string fileName, string contentType, byte[] bytes)
    {
        using var client = Factory.CreateClientAs(user);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(requestId.ToString(CultureInfo.InvariantCulture)), "leaveRequestId");
        form.Add(FilePart(fileName, contentType, bytes, nameAsFileNameStar: false));
        using var response = await client.PostAsync(LeaveAttachments, form);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private async Task<(HttpStatusCode Status, string Body)> UploadLogoAsync(int orgId, string fileName, string contentType, byte[] bytes)
    {
        using var client = Factory.CreateClientAs(TestUsers.Admin);
        using var form = new MultipartFormDataContent();
        form.Add(FilePart(fileName, contentType, bytes, nameAsFileNameStar: false));
        using var response = await client.PostAsync($"/api/Organizations/{orgId}/logo", form);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static MultipartFormDataContent DocumentForm()
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(E1.ToString(CultureInfo.InvariantCulture)), "employeeId");
        form.Add(new StringContent(DocumentTypeId.ToString(CultureInfo.InvariantCulture)), "documentTypeId");
        form.Add(new StringContent("B10 document"), "name");
        return form;
    }

    /// <summary>
    /// The file part. With <paramref name="nameAsFileNameStar"/> the name is sent as RFC 5987 <c>filename*</c>
    /// (which can carry CR, LF and other characters a plain header value cannot), next to a plain ASCII
    /// <c>filename</c> that the server ignores when <c>filename*</c> is present.
    /// </summary>
    private static ByteArrayContent FilePart(string fileName, string contentType, byte[] bytes, bool nameAsFileNameStar)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        file.Headers.ContentDisposition = nameAsFileNameStar
            ? new ContentDispositionHeaderValue("form-data") { Name = "\"file\"", FileName = "\"plain.pdf\"", FileNameStar = fileName }
            : new ContentDispositionHeaderValue("form-data") { Name = "\"file\"", FileName = $"\"{fileName}\"" };
        return file;
    }

    private async Task<HttpResponseMessage> GetAsync(TestUser user, string path, HttpStatusCode expected)
    {
        var client = Factory.CreateClientAs(user);
        var response = await client.GetAsync(path);
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"{user.Description} -> GET {path}: expected {(int)expected}, got {(int)response.StatusCode}. Body starts: {Preview(body)}");
        }

        return response;
    }

    private static async Task AssertDownloadAsync(HttpResponseMessage response, byte[] expectedBody, string expectedType, string expectedName)
    {
        var body = await response.Content.ReadAsByteArrayAsync();
        var disposition = response.Content.Headers.ContentDisposition;
        var rawDisposition = response.Content.Headers.TryGetValues("Content-Disposition", out var values) ? string.Join(",", values) : string.Empty;

        Assert.Multiple(() =>
        {
            Assert.That(body, Is.EqualTo(expectedBody));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(expectedType));
            Assert.That(disposition?.DispositionType, Is.EqualTo("attachment"));
            Assert.That(disposition?.FileName?.Trim('"'), Is.EqualTo(expectedName), "filename");
            Assert.That(disposition?.FileNameStar, Is.EqualTo(expectedName), "filename*");
            Assert.That(rawDisposition, Does.Not.Contain("\r").And.Not.Contain("\n"), "No header injection.");
            Assert.That(NoSniff(response), Is.EqualTo("nosniff"));
        });
    }

    private static string? NoSniff(HttpResponseMessage response)
        => response.Headers.TryGetValues("X-Content-Type-Options", out var values) ? string.Join(",", values) : null;

    private Task<Document> DocumentRowAsync(int id)
        => Factory.QueryDatabaseAsync(db => db.Documents.AsNoTracking().SingleAsync(d => d.Id == id));

    private Task<LeaveRequestAttachment> AttachmentRowAsync(int id)
        => Factory.QueryDatabaseAsync(db => db.LeaveRequestAttachments.AsNoTracking().SingleAsync(a => a.Id == id));

    private Task<string?> LogoPathAsync(int orgId)
        => Factory.QueryDatabaseAsync(db => db.Organizations.AsNoTracking()
            .Where(o => o.Id == orgId).Select(o => o.LogoRelativePath).SingleAsync());

    private static int Id(string body) => JsonDocument.Parse(body).RootElement.GetProperty("id").GetInt32();

    private static string? Code(string body) => Property(body, "code");

    private static string? Message(string body) => Property(body, "message");

    private static string? Property(string body, string name)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty(name, out var value)
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Document LegacyDocument(int id, string key, string contentType, string name, DateTime now) => new()
    {
        Id = id,
        DocumentTypeId = DocumentTypeId,
        Name = $"B10 legacy document {id}",
        FileKind = DocumentFileKind.Pdf,
        OriginalFileName = name,
        ContentType = contentType,
        StoredRelativePath = key,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    private static LeaveRequestAttachment LegacyAttachment(int id, string key, string contentType, string name, string content, DateTime now) => new()
    {
        Id = id,
        LeaveRequestId = RequestE1,
        FileName = name,
        ContentType = contentType,
        FileSizeBytes = Encoding.UTF8.GetByteCount(content),
        StoragePath = key,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    private static LeaveRequest NewLeaveRequest(int id, int employeeId, DateTime now) => new()
    {
        Id = id,
        OrganizationId = TestSeed.O1.Id,
        EmployeeId = employeeId,
        LeaveTypeId = LeaveTypeId,
        StartDateUtc = new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDateUtc = new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        Unit = LeaveUnit.Days,
        RequestedAmount = 1m,
        Reason = "B10 seed",
        Status = LeaveRequestStatus.Pending,
        SubmittedAtUtc = now,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    private string PrivatePath(string key)
        => Path.Combine(Factory.PrivateRootPath, key.Replace('/', Path.DirectorySeparatorChar));

    private string WebPath(string relative)
        => Path.Combine(Factory.WebRootPath, relative.Replace('/', Path.DirectorySeparatorChar));

    private static string EnsureFolder(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    private static void WriteFile(string path, string content) => File.WriteAllText(EnsureFolder(path), content);

    private static int CountFiles(string root)
        => Directory.Exists(root) ? Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length : 0;

    private static string Preview(string body)
        => string.IsNullOrEmpty(body) ? "(empty)" : body.Length <= 400 ? body : body[..400] + "...";
}
