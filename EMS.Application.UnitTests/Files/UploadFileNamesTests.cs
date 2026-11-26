using System.Text;
using EMS.Application.Services.Files;

namespace EMS.Application.UnitTests.Files;

/// <summary>B10: display names keep only a clean base name plus the server's extension (N01 to N09).</summary>
[TestFixture]
public sealed class UploadFileNamesTests
{
    private static string Sanitise(string? name, string ext = ".pdf") => UploadFileNames.SanitiseDisplayName(name, ext, "document");

    [Test]
    public void N01_WindowsFakePath_IsDropped() => Assert.That(Sanitise("C:\\fakepath\\contract.pdf"), Is.EqualTo("contract.pdf"));

    [Test]
    public void N02_RelativePathParts_AreDropped() => Assert.That(Sanitise("../../etc/passwd.pdf"), Is.EqualTo("passwd.pdf"));

    [Test]
    public void N03_ControlCharacters_AreRemoved() => Assert.That(Sanitise("a\r\nb\t.pdf"), Is.EqualTo("ab.pdf"));

    [Test]
    public void N04_BidiOverride_IsRemoved()
    {
        var result = Sanitise("invoice\u202Efdp.exe.pdf");
        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Not.Contain("\u202E"));
            Assert.That(result, Does.EndWith(".pdf"));
            Assert.That(result, Is.EqualTo("invoicefdp.exe.pdf"));
        });
    }

    [Test]
    public void N04b_OtherFormatCharacters_AreRemoved()
        => Assert.That(Sanitise("a\u200Bb\u2066c\u2069\uFEFF.pdf"), Is.EqualTo("abc.pdf"));

    [Test]
    public void N05_QuotesAreReplacedAndTheExtensionIsTheServers()
        => Assert.That(Sanitise("Contract \"final\".PDF"), Is.EqualTo("Contract _final_.pdf"));

    [Test]
    public void N05b_RequirementsExample()
        => Assert.That(Sanitise("C:\\fakepath\\Contract \"final\"\u202E.PDF"), Is.EqualTo("Contract _final_.pdf"));

    [Test]
    public void N05c_OtherUnsafeCharacters_AreReplaced()
        => Assert.That(Sanitise("a*b<c>d?e|f.pdf"), Is.EqualTo("a_b_c_d_e_f.pdf"));

    [Test]
    public void N06_LongBase_IsCappedAt120Characters()
    {
        var result = Sanitise(new string('a', 300) + ".pdf");
        Assert.That(result, Is.EqualTo(new string('a', 120) + ".pdf"));
    }

    [Test]
    public void N06b_CapDoesNotSplitASurrogatePair()
    {
        // 119 letters, then an emoji (two chars) that straddles the 120-character cut.
        var result = Sanitise(new string('a', 119) + "\U0001F600" + "tail.pdf");
        var baseName = result[..^4];
        Assert.Multiple(() =>
        {
            Assert.That(baseName, Has.Length.LessThanOrEqualTo(UploadFileNames.MaxBaseLength));
            Assert.That(char.IsHighSurrogate(baseName[^1]), Is.False, "The cut must not leave a lone high surrogate.");
            Assert.That(result, Does.EndWith(".pdf"));
        });
    }

    [TestCase("   .pdf")]
    [TestCase("")]
    [TestCase(null)]
    [TestCase("...")]
    [TestCase("\u202E\r\n")]
    public void N07_NothingLeft_UsesTheFallback(string? name) => Assert.That(Sanitise(name), Is.EqualTo("document.pdf"));

    [Test]
    public void N08_DecomposedCharacters_AreNormalisedToNfc()
    {
        var result = Sanitise("Re\u0301sume\u0301.pdf");
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("R\u00E9sum\u00E9.pdf"));
            Assert.That(result.IsNormalized(NormalizationForm.FormC), Is.True);
        });
    }

    [TestCase("C:\\fakepath\\Contract \"final\"\u202E.PDF")]
    [TestCase("a\r\nb\t.pdf")]
    [TestCase("report.exe.pdf")]
    [TestCase("   .pdf")]
    public void N09_SanitisingTwice_ChangesNothing(string name)
    {
        var once = Sanitise(name);
        Assert.That(Sanitise(once), Is.EqualTo(once));
    }

    [Test]
    public void N09b_LongNameIsIdempotent()
    {
        var once = Sanitise(new string('b', 300) + ".pdf");
        Assert.That(Sanitise(once), Is.EqualTo(once));
    }

    [Test]
    public void LoneSurrogate_IsRemovedWithoutAnException()
        => Assert.That(Sanitise("a\uD800b.pdf"), Is.EqualTo("ab.pdf"));

    [TestCase("report.pdf", ".pdf")]
    [TestCase("report.exe.pdf", ".pdf")]
    [TestCase("report.pdf.exe", ".exe")]
    [TestCase("A.PDF", ".pdf")]
    [TestCase("invoice\u202Efdp.exe", ".exe")]
    [TestCase("noextension", "")]
    [TestCase(null, "")]
    public void ExtensionOf_ReturnsTheLastExtensionOfTheCleanedName(string? name, string expected)
        => Assert.That(UploadFileNames.ExtensionOf(name), Is.EqualTo(expected));

    [TestCase("Leave/12/b.txt", ".txt")]
    [TestCase("Employee/5/Documents/3f2a.PDF", ".pdf")]
    [TestCase("Employee/5/Documents/3f2a.docx", ".docx")]
    [TestCase("/attachments/Leave/1/a.jpeg", ".jpeg")]
    [TestCase("Leave/12/b", "")]
    [TestCase("Leave/12/b.toolong", "")]
    [TestCase("Leave/12/b.p-f", "")]
    [TestCase(null, "")]
    public void ServerExtensionOfKey_AcceptsOnlyPlainShortExtensions(string? key, string expected)
        => Assert.That(UploadFileNames.ServerExtensionOfKey(key), Is.EqualTo(expected));
}
