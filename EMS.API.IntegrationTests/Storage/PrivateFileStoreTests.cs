using System.Text;
using EMS.API.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EMS.API.IntegrationTests.Storage;

/// <summary>
/// B08: <see cref="PrivateFileStore"/> path safety (U1 to U5). Plain unit tests with a temporary root; no host.
/// </summary>
[TestFixture]
public sealed class PrivateFileStoreTests
{
    private TempFolder _temp = null!;
    private PrivateFileStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _temp = new TempFolder();
        _store = new PrivateFileStore(_temp.Combine("private"), NullLogger<PrivateFileStore>.Instance);
    }

    [TearDown]
    public void TearDown() => _temp.Dispose();

    [TestCase("Employee/5/Documents/a.pdf", "Employee/5/Documents/a.pdf")]
    [TestCase("Leave/12/a.txt", "Leave/12/a.txt")]
    [TestCase("/attachments/Leave/12/a.txt", "Leave/12/a.txt")]
    [TestCase("Leave\\12\\a.txt", "Leave/12/a.txt")]
    public void U1_TryResolve_AcceptsSafeKeys(string stored, string expectedKey)
    {
        var ok = _store.TryResolve(stored, out var full);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True, stored);
            Assert.That(full, Is.EqualTo(Path.GetFullPath(Path.Combine(_store.RootPath,
                expectedKey.Replace('/', Path.DirectorySeparatorChar)))));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("../x")]
    [TestCase("Leave/../../x")]
    [TestCase("Leave/./a")]
    [TestCase("Leave//a")]
    [TestCase("/etc/passwd")]
    [TestCase("C:\\x")]
    [TestCase("\\\\server\\share\\x")]
    [TestCase("Organization/1/logo.png")]
    [TestCase("uploads/x")]
    [TestCase("Leave/a\0b")]
    [TestCase("/attachments/Organization/1/logo.png")]
    [TestCase("leave/12/a.txt")]
    [TestCase("Leave/12/")]
    public void U2_TryResolve_RejectsUnsafeKeys(string? stored)
    {
        Assert.That(_store.TryResolve(stored, out var full), Is.False, stored ?? "(null)");
        Assert.That(full, Is.Empty);
    }

    [Test]
    public void U3_SiblingFolderWithSamePrefix_IsNotInsideRoot()
    {
        var root = _temp.Combine("private");
        var sibling = Path.GetFullPath(_temp.Combine("private-evil/a"));

        Assert.Multiple(() =>
        {
            Assert.That(PrivateFileStore.IsInside(sibling, root), Is.False, "B06 regression: no trailing separator check.");
            Assert.That(PrivateFileStore.IsInside(Path.GetFullPath(_temp.Combine("private/Leave/a")), root), Is.True);
            Assert.That(PrivateFileStore.IsInside(Path.GetFullPath(root), root), Is.False, "The root itself is not a file inside it.");
            Assert.That(_store.TryResolve("Leave/../../private-evil/Leave/a", out _), Is.False);
        });
    }

    [Test]
    public async Task U4_SaveAsync_WritesInsideRootAndReturnsKey()
    {
        var bytes = Encoding.UTF8.GetBytes("U4 content");
        using var stream = new MemoryStream(bytes);

        // B10: the extension is the server's (from the upload checker), never the uploaded file name's.
        var key = await _store.SaveAsync("Employee/5/Documents", stream, ".pdf");

        Assert.Multiple(() =>
        {
            Assert.That(key, Does.Match("^Employee/5/Documents/[0-9a-f]{32}\\.pdf$"));
            Assert.That(_store.TryResolve(key, out var full), Is.True);
            Assert.That(File.ReadAllBytes(full), Is.EqualTo(bytes));
            Assert.That(PrivateFileStore.IsInside(full, _store.RootPath), Is.True);
        });
    }

    [Test]
    public void U4_SaveAsync_RefusesUnsafeFolder()
    {
        using var stream = new MemoryStream([1]);

        Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveAsync("Leave/../..", stream, ".pdf"));
        Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveAsync("Organization/1", stream, ".pdf"));
    }

    [TestCase(".PDF")]
    [TestCase(".html.")]
    [TestCase("pdf")]
    [TestCase(".p")]
    [TestCase(".exe/../x")]
    [TestCase("")]
    [TestCase(null)]
    public void B10_SaveAsync_RefusesAnExtensionThatIsNotAServerExtension(string? extension)
    {
        using var stream = new MemoryStream([1]);

        Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveAsync("Leave/12", stream, extension!));
        Assert.That(Directory.Exists(_temp.Combine("private/Leave/12")) &&
                    Directory.GetFiles(_temp.Combine("private/Leave/12")).Length > 0, Is.False, "Nothing may be written.");
    }

    [Test]
    public void U5_TryDelete_WithUnsafeKey_DeletesNothing()
    {
        var sentinel = _temp.Write("x", "sentinel");
        var siblingSentinel = _temp.Write("private-evil/Leave/a.txt", "sentinel");
        var logo = _temp.Write("private/Organization/1/logo.png", "logo");

        _store.TryDelete("Leave/../../x");
        _store.TryDelete("../x");
        _store.TryDelete(sentinel);
        _store.TryDelete("Leave/../../private-evil/Leave/a.txt");
        _store.TryDelete("Organization/1/logo.png");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(sentinel), Is.True);
            Assert.That(File.Exists(siblingSentinel), Is.True);
            Assert.That(File.Exists(logo), Is.True);
        });
    }

    [Test]
    public void U5_TryDelete_WithSafeKey_DeletesTheFile()
    {
        var file = _temp.Write("private/Leave/12/a.txt", "x");
        _store.TryDelete("Leave/12/a.txt");
        Assert.That(File.Exists(file), Is.False);
    }

    [Test]
    public void S1_ResolveRoot_InsideWebRoot_Throws()
    {
        var webRoot = _temp.Combine("wwwroot");

        var inside = Assert.Throws<InvalidOperationException>(
            () => PrivateFileStore.ResolveRoot("wwwroot/private", _temp.Path, webRoot));
        var same = Assert.Throws<InvalidOperationException>(
            () => PrivateFileStore.ResolveRoot(webRoot, _temp.Path, webRoot));

        Assert.Multiple(() =>
        {
            Assert.That(inside!.Message, Does.Contain("must not be the web root"));
            Assert.That(same!.Message, Does.Contain("must not be the web root"));
        });
    }

    [Test]
    public void S1_ResolveRoot_RelativeResolvesAgainstContentRoot_AbsoluteUsedAsGiven()
    {
        var webRoot = _temp.Combine("wwwroot");
        var absolute = _temp.Combine("elsewhere/private");

        Assert.Multiple(() =>
        {
            Assert.That(PrivateFileStore.ResolveRoot("App_Data/private", _temp.Path, webRoot),
                Is.EqualTo(Path.GetFullPath(_temp.Combine("App_Data/private"))));
            Assert.That(PrivateFileStore.ResolveRoot(null, _temp.Path, webRoot),
                Is.EqualTo(Path.GetFullPath(_temp.Combine("App_Data/private"))));
            Assert.That(PrivateFileStore.ResolveRoot(absolute, _temp.Path, webRoot), Is.EqualTo(Path.GetFullPath(absolute)));
            Assert.That(PrivateFileStore.ResolveRoot("wwwroot-private", _temp.Path, webRoot),
                Is.EqualTo(Path.GetFullPath(_temp.Combine("wwwroot-private"))), "A sibling of the web root is allowed.");
        });
    }
}
