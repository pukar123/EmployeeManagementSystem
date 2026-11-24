using EMS.API.Services;
using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EMS.API.IntegrationTests.Storage;

/// <summary>
/// B08: the one-off move from <c>wwwroot/attachments</c> into the private root (M1 to M7). EF Core InMemory and
/// temporary web and private roots; no host.
/// </summary>
[TestFixture]
public sealed class PublicToPrivateFileMoverTests
{
    private TempFolder _temp = null!;
    private string _databaseName = string.Empty;
    private PrivateFileStore _store = null!;
    private TestWebHostEnvironment _environment = null!;

    [SetUp]
    public void SetUp()
    {
        _temp = new TempFolder();
        _databaseName = $"ems-mover-tests-{Guid.NewGuid():N}";
        _store = new PrivateFileStore(_temp.Combine("private"), NullLogger<PrivateFileStore>.Instance);
        _environment = new TestWebHostEnvironment(_temp.Combine("wwwroot"), _temp.Path);
    }

    [TearDown]
    public void TearDown() => _temp.Dispose();

    [Test]
    public async Task M1_LegacyRowsWithFiles_AreMovedAndKeysUpdated()
    {
        var docSource = _temp.Write("wwwroot/attachments/Employee/5/Documents/a.pdf", "doc");
        var leaveSource = _temp.Write("wwwroot/attachments/Leave/12/b.txt", "leave");
        await SeedAsync(
            [Doc(1, "/attachments/Employee/5/Documents/a.pdf")],
            [Attachment(1, "/attachments/Leave/12/b.txt")]);

        var result = await MoveAsync();

        var stored1 = await DocumentPathAsync(1);
        var stored2 = await AttachmentPathAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Moved, Is.EqualTo(2));
            Assert.That(result.PathsUpdated, Is.EqualTo(2));
            Assert.That(result.Missing + result.Skipped + result.Conflicts, Is.Zero);
            Assert.That(File.Exists(docSource), Is.False);
            Assert.That(File.Exists(leaveSource), Is.False);
            Assert.That(File.ReadAllText(_temp.Combine("private/Employee/5/Documents/a.pdf")), Is.EqualTo("doc"));
            Assert.That(File.ReadAllText(_temp.Combine("private/Leave/12/b.txt")), Is.EqualTo("leave"));
            Assert.That(stored1, Is.EqualTo("Employee/5/Documents/a.pdf"));
            Assert.That(stored2, Is.EqualTo("Leave/12/b.txt"));
        });
    }

    [Test]
    public async Task M2_SecondRun_MovesAndUpdatesNothing()
    {
        _temp.Write("wwwroot/attachments/Employee/5/Documents/a.pdf", "doc");
        _temp.Write("wwwroot/attachments/Leave/12/b.txt", "leave");
        await SeedAsync(
            [Doc(1, "/attachments/Employee/5/Documents/a.pdf")],
            [Attachment(1, "/attachments/Leave/12/b.txt")]);
        await MoveAsync();

        var second = await MoveAsync();

        var stored3 = await DocumentPathAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(second.Moved, Is.Zero);
            Assert.That(second.PathsUpdated, Is.Zero);
            Assert.That(second.AlreadyPrivate, Is.EqualTo(2));
            Assert.That(second.OrphansMoved + second.Missing + second.Skipped + second.Conflicts, Is.Zero);
            Assert.That(File.ReadAllText(_temp.Combine("private/Employee/5/Documents/a.pdf")), Is.EqualTo("doc"));
            Assert.That(File.ReadAllText(_temp.Combine("private/Leave/12/b.txt")), Is.EqualTo("leave"));
            Assert.That(stored3, Is.EqualTo("Employee/5/Documents/a.pdf"));
        });
    }

    [Test]
    public async Task M3_MissingSourceFile_LeavesRowAndOthersStillMove()
    {
        _temp.Write("wwwroot/attachments/Leave/12/b.txt", "leave");
        await SeedAsync(
            [Doc(1, "/attachments/Employee/5/Documents/gone.pdf")],
            [Attachment(1, "/attachments/Leave/12/b.txt")]);

        var result = await MoveAsync();

        var stored4 = await DocumentPathAsync(1);
        var stored5 = await AttachmentPathAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Missing, Is.EqualTo(1));
            Assert.That(result.Moved, Is.EqualTo(1));
            Assert.That(stored4, Is.EqualTo("/attachments/Employee/5/Documents/gone.pdf"), "Row unchanged.");
            Assert.That(stored5, Is.EqualTo("Leave/12/b.txt"));
        });
    }

    [Test]
    public async Task M4_TargetExistsWithSameLength_SourceDeletedAndRowUpdated()
    {
        var source = _temp.Write("wwwroot/attachments/Leave/12/b.txt", "same");
        var target = _temp.Write("private/Leave/12/b.txt", "SAME");
        await SeedAsync([], [Attachment(1, "/attachments/Leave/12/b.txt")]);

        var result = await MoveAsync();

        var stored6 = await AttachmentPathAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.PathsUpdated, Is.EqualTo(1));
            Assert.That(result.Moved, Is.Zero);
            Assert.That(result.Conflicts, Is.Zero);
            Assert.That(File.Exists(source), Is.False);
            Assert.That(File.ReadAllText(target), Is.EqualTo("SAME"), "The private file is never overwritten.");
            Assert.That(stored6, Is.EqualTo("Leave/12/b.txt"));
        });
    }

    [Test]
    public async Task M4_TargetExistsWithDifferentLength_ConflictAndBothKept()
    {
        var source = _temp.Write("wwwroot/attachments/Leave/12/b.txt", "longer public copy");
        var target = _temp.Write("private/Leave/12/b.txt", "short");
        await SeedAsync([], [Attachment(1, "/attachments/Leave/12/b.txt")]);

        var result = await MoveAsync();

        var stored7 = await AttachmentPathAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Conflicts, Is.EqualTo(1));
            Assert.That(result.PathsUpdated, Is.EqualTo(1));
            Assert.That(File.ReadAllText(source), Is.EqualTo("longer public copy"));
            Assert.That(File.ReadAllText(target), Is.EqualTo("short"));
            Assert.That(stored7, Is.EqualTo("Leave/12/b.txt"));
        });
    }

    [Test]
    public async Task M5_OrphanFile_IsMovedToPrivateRoot()
    {
        var orphan = _temp.Write("wwwroot/attachments/Leave/99/orphan.txt", "orphan");
        var employeeOrphan = _temp.Write("wwwroot/attachments/Employee/7/Documents/o.pdf", "orphan doc");
        await SeedAsync([], []);

        var result = await MoveAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.OrphansMoved, Is.EqualTo(2));
            Assert.That(File.Exists(orphan), Is.False);
            Assert.That(File.Exists(employeeOrphan), Is.False);
            Assert.That(File.ReadAllText(_temp.Combine("private/Leave/99/orphan.txt")), Is.EqualTo("orphan"));
            Assert.That(File.ReadAllText(_temp.Combine("private/Employee/7/Documents/o.pdf")), Is.EqualTo("orphan doc"));
        });
    }

    [Test]
    public async Task M6_UnsafeLegacyKey_IsSkippedAndNothingTouched()
    {
        var sentinel = _temp.Write("x", "sentinel");
        await SeedAsync([], [Attachment(1, "/attachments/Leave/../../x")]);

        var result = await MoveAsync();

        var stored8 = await AttachmentPathAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Skipped, Is.EqualTo(1));
            Assert.That(result.Moved + result.PathsUpdated, Is.Zero);
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("sentinel"));
            Assert.That(stored8, Is.EqualTo("/attachments/Leave/../../x"));
        });
    }

    [Test]
    public async Task M7_LogoFiles_AreNeverTouched()
    {
        var logo = _temp.Write("wwwroot/attachments/Organization/1/logo-a.png", "logo");
        await SeedAsync([], []);

        var result = await MoveAsync();

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(logo), Is.EqualTo("logo"));
            Assert.That(Directory.Exists(_temp.Combine("private/Organization")), Is.False);
            Assert.That(result.OrphansMoved + result.Moved, Is.Zero);
        });
    }

    private async Task<FileMoveResult> MoveAsync()
    {
        await using var db = CreateDb();
        var mover = new PublicToPrivateFileMover(db, _environment, _store, NullLogger<PublicToPrivateFileMover>.Instance);
        return await mover.MoveAsync();
    }

    private async Task SeedAsync(Document[] documents, LeaveRequestAttachment[] attachments)
    {
        await using var db = CreateDb();
        db.Documents.AddRange(documents);
        db.LeaveRequestAttachments.AddRange(attachments);
        await db.SaveChangesAsync();
    }

    private async Task<string> DocumentPathAsync(int id)
    {
        await using var db = CreateDb();
        return await db.Documents.AsNoTracking().Where(d => d.Id == id).Select(d => d.StoredRelativePath).SingleAsync();
    }

    private async Task<string> AttachmentPathAsync(int id)
    {
        await using var db = CreateDb();
        return await db.LeaveRequestAttachments.AsNoTracking().Where(a => a.Id == id).Select(a => a.StoragePath).SingleAsync();
    }

    private AppDbContext CreateDb()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(_databaseName).Options);

    private static Document Doc(int id, string path) => new()
    {
        Id = id,
        DocumentTypeId = 1,
        Name = $"Doc {id}",
        FileKind = DocumentFileKind.Pdf,
        OriginalFileName = "a.pdf",
        ContentType = "application/pdf",
        StoredRelativePath = path,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
    };

    private static LeaveRequestAttachment Attachment(int id, string path) => new()
    {
        Id = id,
        LeaveRequestId = 12,
        FileName = "b.txt",
        ContentType = "text/plain",
        FileSizeBytes = 1,
        StoragePath = path,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
    };
}
