using EMS.API.Services;

namespace EMS.API.IntegrationTests.Storage;

/// <summary>B10: download content types come only from an allow-list, and download names are sanitised.</summary>
[TestFixture]
public sealed class FileDownloadHeadersTests
{
    [TestCase("application/pdf", "application/pdf")]
    [TestCase("Application/PDF; charset=binary", "application/pdf")]
    [TestCase("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [TestCase("application/msword", "application/msword")]
    [TestCase("image/png", "image/png")]
    [TestCase("image/jpeg", "image/jpeg")]
    [TestCase("image/gif", "image/gif")]
    [TestCase("image/webp", "image/webp")]
    [TestCase("image/bmp", "image/bmp")]
    public void SafeContentType_AllowedTypes_PassThrough(string stored, string expected)
        => Assert.That(FileDownloadHeaders.SafeContentType(stored), Is.EqualTo(expected));

    [TestCase("text/html")]
    [TestCase("image/svg+xml")]
    [TestCase("text/plain")]
    [TestCase("application/javascript")]
    [TestCase("not a type")]
    [TestCase("")]
    [TestCase(null)]
    public void SafeContentType_AnythingElse_IsOctetStream(string? stored)
        => Assert.That(FileDownloadHeaders.SafeContentType(stored), Is.EqualTo("application/octet-stream"));

    [Test]
    public void SafeFileName_LegacyName_IsSanitisedWithTheKeysExtension()
        => Assert.That(FileDownloadHeaders.SafeFileName("C:\\x\\a\"\r\nb\u202E.html", "Leave/12/b.txt"), Is.EqualTo("a_b.txt"));

    [Test]
    public void SafeFileName_CleanName_IsUnchanged()
        => Assert.That(FileDownloadHeaders.SafeFileName("b08 evidence.txt", "Leave/8801/b08-seeded.txt"), Is.EqualTo("b08 evidence.txt"));

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void SafeFileName_Empty_IsDownload(string? name)
        => Assert.That(FileDownloadHeaders.SafeFileName(name, "Leave/12/noext"), Is.EqualTo("download"));

    [Test]
    public void SafeFileName_Empty_KeepsTheKeysExtension()
        => Assert.That(FileDownloadHeaders.SafeFileName("", "Employee/5/Documents/abc.pdf"), Is.EqualTo("download.pdf"));
}
