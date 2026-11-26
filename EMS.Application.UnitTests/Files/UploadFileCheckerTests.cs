using EMS.Application.DTOs.Files;
using EMS.Application.Services.Files;
using EMS.Domain.Enums;

namespace EMS.Application.UnitTests.Files;

/// <summary>
/// B10: the shared upload checker. A file passes only when its extension, declared content type and content
/// signature all match one allowed type for the purpose (U01 to U08, D01 to D17, S01 to S06).
/// </summary>
[TestFixture]
public sealed class UploadFileCheckerTests
{
    private const long FifteenMegabytes = 15L * 1024 * 1024;
    private const long FiveMegabytes = 5L * 1024 * 1024;

    private readonly UploadFileChecker _checker = new();

    [Test]
    public void U01_DocumentPdf_IsAccepted()
    {
        var result = Check(UploadPurpose.EmployeeDocument, "a.pdf", "application/pdf", TestFiles.Pdf());
        AssertAccepted(result, ".pdf", "application/pdf", DocumentFileKind.Pdf, "a.pdf");
    }

    [Test]
    public void U02_DocumentDocx_IsAccepted()
    {
        var result = Check(UploadPurpose.EmployeeDocument, "a.docx", TestFiles.DocxType, TestFiles.Docx());
        AssertAccepted(result, ".docx", TestFiles.DocxType, DocumentFileKind.Word, "a.docx");
    }

    [Test]
    public void U03_DocumentPng_IsAccepted()
    {
        var result = Check(UploadPurpose.EmployeeDocument, "a.png", "image/png", TestFiles.Png());
        AssertAccepted(result, ".png", "image/png", DocumentFileKind.Image, "a.png");
    }

    [Test]
    public void U04_DocumentJpegWithImageJpg_IsAcceptedAsJpg()
    {
        var result = Check(UploadPurpose.EmployeeDocument, "a.jpeg", "image/jpg", TestFiles.Jpeg());
        AssertAccepted(result, ".jpg", "image/jpeg", DocumentFileKind.Image, "a.jpg");
    }

    [Test]
    public void U05_UpperCaseExtensionAndContentTypeWithParameters_AreAccepted()
    {
        var result = Check(UploadPurpose.EmployeeDocument, "A.PDF", "Application/PDF; charset=binary", TestFiles.Pdf());
        AssertAccepted(result, ".pdf", "application/pdf", DocumentFileKind.Pdf, "A.pdf");
    }

    private static IEnumerable<TestCaseData> LeaveAccepted()
    {
        yield return new TestCaseData("note.pdf", "application/pdf", TestFiles.Pdf(), ".pdf", "application/pdf").SetName("U06a leave PDF accepted");
        yield return new TestCaseData("scan.png", "image/png", TestFiles.Png(), ".png", "image/png").SetName("U06b leave PNG accepted");
        yield return new TestCaseData("photo.jpg", "image/jpeg", TestFiles.Jpeg(), ".jpg", "image/jpeg").SetName("U06c leave JPEG accepted");
    }

    [TestCaseSource(nameof(LeaveAccepted))]
    public void U06_LeaveAttachment_AllowedTypes_AreAccepted(string name, string type, byte[] bytes, string ext, string canonical)
    {
        var result = Check(UploadPurpose.LeaveAttachment, name, type, bytes);
        Assert.Multiple(() =>
        {
            Assert.That(result.Extension, Is.EqualTo(ext));
            Assert.That(result.ContentType, Is.EqualTo(canonical));
        });
    }

    [Test]
    public void U07_LeaveAttachment_Docx_IsRefused()
        => AssertRefused(UploadPurpose.LeaveAttachment, "a.docx", TestFiles.DocxType, TestFiles.Docx(), UploadRejectionReason.TypeNotAllowed);

    private static IEnumerable<TestCaseData> LogoAccepted()
    {
        yield return new TestCaseData("logo.png", "image/png", TestFiles.Png(), ".png").SetName("U08a logo PNG accepted");
        yield return new TestCaseData("logo.jpg", "image/jpeg", TestFiles.Jpeg(), ".jpg").SetName("U08b logo JPEG accepted");
        yield return new TestCaseData("logo.webp", "image/webp", TestFiles.WebP(), ".webp").SetName("U08c logo WebP accepted");
    }

    [TestCaseSource(nameof(LogoAccepted))]
    public void U08_Logo_AllowedTypes_AreAcceptedWithNoFileKind(string name, string type, byte[] bytes, string ext)
    {
        var result = Check(UploadPurpose.OrganizationLogo, name, type, bytes);
        Assert.Multiple(() =>
        {
            Assert.That(result.Extension, Is.EqualTo(ext));
            Assert.That(result.FileKind, Is.EqualTo(DocumentFileKind.None));
        });
    }

    private static IEnumerable<TestCaseData> Disguised()
    {
        var doc = UploadPurpose.EmployeeDocument;
        yield return Refused("D01 HTML named .pdf", doc, "x.pdf", "application/pdf", TestFiles.Html());
        yield return Refused("D02 HTML named .html declared PDF", doc, "x.html", "application/pdf", TestFiles.Html());
        yield return Refused("D03 SVG logo", UploadPurpose.OrganizationLogo, "logo.svg", "image/svg+xml", TestFiles.Svg());
        yield return Refused("D04 SVG text named .png logo", UploadPurpose.OrganizationLogo, "logo.png", "image/png", TestFiles.Svg());
        yield return Refused("D05 EXE named .png leave", UploadPurpose.LeaveAttachment, "cert.png", "image/png", TestFiles.Exe());
        yield return Refused("D06 zip with only readme.txt as .docx", doc, "a.docx", TestFiles.DocxType, TestFiles.Zip("readme.txt"));
        yield return Refused("D07 docx with macros", doc, "a.docx", TestFiles.DocxType, TestFiles.Docx("word/vbaProject.bin"));
        yield return Refused("D08 zip signature then garbage", doc, "a.docx", TestFiles.DocxType, TestFiles.BrokenZip());
        yield return Refused("D09 xlsx named .docx", doc, "a.docx", TestFiles.DocxType, TestFiles.Zip("[Content_Types].xml", "xl/workbook.xml"));
        yield return Refused("D10 PDF named .png", doc, "a.png", "image/png", TestFiles.Pdf());
        yield return Refused("D11 PDF declared image/png", doc, "a.pdf", "image/png", TestFiles.Pdf());
        yield return Refused("D12a PDF declared octet-stream", doc, "a.pdf", "application/octet-stream", TestFiles.Pdf());
        yield return Refused("D12b PDF with empty declared type", doc, "a.pdf", "", TestFiles.Pdf());
        yield return Refused("D12c PDF with no declared type", doc, "a.pdf", null, TestFiles.Pdf());
        yield return Refused("D13 legacy .doc", doc, "a.doc", "application/msword", TestFiles.Ole2());
        yield return Refused("D14 report.pdf.exe", doc, "report.pdf.exe", "application/pdf", TestFiles.Pdf());
        yield return Refused("D16 three bytes of PNG", doc, "a.png", "image/png", [0x89, 0x50, 0x4E]);
        yield return Refused("D17 GIF document", doc, "a.gif", "image/gif", TestFiles.Gif());
        yield return Refused("D18 bidi name ending .exe", doc, "invoice\u202Efdp.exe", "application/pdf", TestFiles.Pdf());
        yield return Refused("D19 docx bytes named .pdf", doc, "a.pdf", "application/pdf", TestFiles.Docx());
        yield return Refused("D20 WebP document", doc, "a.webp", "image/webp", TestFiles.WebP());
        yield return Refused("D21 PNG declared image/jpeg", doc, "a.png", "image/jpeg", TestFiles.Png());
        yield return Refused("D22 docx with word/VBAProject.bin (other case)", doc, "a.docx", TestFiles.DocxType, TestFiles.Docx("word/VBAProject.bin"));
        yield return Refused("D23 docx with 10,001 entries", doc, "a.docx", TestFiles.DocxType, TestFiles.DocxWithEntryCount(10_001));
        yield return Refused("D24 ZIP64 docx", doc, "a.docx", TestFiles.DocxType, TestFiles.WithZip64Locator(TestFiles.Docx()));
        yield return Refused("D25 docx whose record declares 0xFFFF entries (ZIP64 marker)", doc, "a.docx", TestFiles.DocxType,
            TestFiles.WithDeclaredEntryCount(TestFiles.Docx(), ushort.MaxValue));
        yield return Refused("D26 docx whose record declares 60,000 entries", doc, "a.docx", TestFiles.DocxType,
            TestFiles.WithDeclaredEntryCount(TestFiles.Docx(), 60_000));
    }

    [TestCaseSource(nameof(Disguised))]
    public void D_DisguisedOrNotAllowedFile_IsRefusedWithoutAnyOtherException(
        UploadPurpose purpose, string name, string? type, byte[] bytes)
        => AssertRefused(purpose, name, type, bytes, UploadRejectionReason.TypeNotAllowed);

    [Test]
    public void D23b_DocxWithExactly10000Entries_IsAccepted()
    {
        var result = Check(UploadPurpose.EmployeeDocument, "a.docx", TestFiles.DocxType, TestFiles.DocxWithEntryCount(10_000));
        Assert.That(result.FileKind, Is.EqualTo(DocumentFileKind.Word));
    }

    [Test]
    public void D26b_DeclaredEntryCountOverTheLimit_IsRefusedAndTheStreamIsReset()
    {
        // The record claims 60,000 entries (over the limit, below the ZIP64 marker). The early record check refuses it
        // with the normal rejection, never an exception, and leaves the stream at position 0.
        var bytes = TestFiles.WithDeclaredEntryCount(TestFiles.Docx(), 60_000);
        using var stream = new MemoryStream(bytes);
        Assert.Throws<UploadRejectedException>(() =>
            _checker.Check(UploadPurpose.EmployeeDocument, stream, "a.docx", TestFiles.DocxType, bytes.Length));
        Assert.That(stream.Position, Is.Zero);
    }

    [Test]
    public void D15_ReportExePdf_IsAcceptedAsPdf()
    {
        var result = Check(UploadPurpose.EmployeeDocument, "report.exe.pdf", "application/pdf", TestFiles.Pdf());
        Assert.Multiple(() =>
        {
            Assert.That(result.Extension, Is.EqualTo(".pdf"));
            Assert.That(result.DisplayFileName, Is.EqualTo("report.exe.pdf"));
        });
    }

    [TestCase(UploadPurpose.EmployeeDocument)]
    [TestCase(UploadPurpose.LeaveAttachment)]
    [TestCase(UploadPurpose.OrganizationLogo)]
    public void S01_EmptyFile_IsRefusedAsEmpty(UploadPurpose purpose)
    {
        var ex = AssertRefused(purpose, "a.png", "image/png", [], UploadRejectionReason.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(ex.Code, Is.EqualTo("file_empty"));
            Assert.That(ex.Message, Is.EqualTo("The file is empty."));
        });
    }

    [TestCase(UploadPurpose.EmployeeDocument)]
    [TestCase(UploadPurpose.LeaveAttachment)]
    public void S02_ExactlyFifteenMegabytes_IsAccepted(UploadPurpose purpose)
    {
        var result = Check(purpose, "big.pdf", "application/pdf", TestFiles.Padded(TestFiles.Pdf(), FifteenMegabytes));
        Assert.That(result.SizeBytes, Is.EqualTo(FifteenMegabytes));
    }

    [TestCase(UploadPurpose.EmployeeDocument)]
    [TestCase(UploadPurpose.LeaveAttachment)]
    public void S03_FifteenMegabytesPlusOne_IsRefusedAsTooLarge(UploadPurpose purpose)
    {
        var ex = AssertRefused(purpose, "big.pdf", "application/pdf",
            TestFiles.Padded(TestFiles.Pdf(), FifteenMegabytes + 1), UploadRejectionReason.TooLarge);
        Assert.Multiple(() =>
        {
            Assert.That(ex.Code, Is.EqualTo("file_too_large"));
            Assert.That(ex.Message, Is.EqualTo("The file is too large. The maximum size is 15 MB."));
        });
    }

    [Test]
    public void S04_LogoFiveMegabytesPlusOne_IsRefusedAsTooLarge()
    {
        var ex = AssertRefused(UploadPurpose.OrganizationLogo, "logo.png", "image/png",
            TestFiles.Padded(TestFiles.Png(), FiveMegabytes + 1), UploadRejectionReason.TooLarge);
        Assert.That(ex.Message, Is.EqualTo("The file is too large. The maximum size is 5 MB."));
    }

    [Test]
    public void S04b_LogoExactlyFiveMegabytes_IsAccepted()
    {
        var result = Check(UploadPurpose.OrganizationLogo, "logo.png", "image/png", TestFiles.Padded(TestFiles.Png(), FiveMegabytes));
        Assert.That(result.Extension, Is.EqualTo(".png"));
    }

    [Test]
    public void S05_StreamIsAtPositionZeroAfterTheCheck()
    {
        var bytes = TestFiles.Pdf();
        using var stream = new MemoryStream(bytes);
        stream.Position = 3;

        _checker.Check(UploadPurpose.EmployeeDocument, stream, "a.pdf", "application/pdf", bytes.Length);

        Assert.That(stream.Position, Is.Zero);
    }

    [Test]
    public void S05b_StreamIsAtPositionZeroAfterARefusedDocx()
    {
        var bytes = TestFiles.BrokenZip();
        using var stream = new MemoryStream(bytes);

        Assert.Throws<UploadRejectedException>(() =>
            _checker.Check(UploadPurpose.EmployeeDocument, stream, "a.docx", TestFiles.DocxType, bytes.Length));
        Assert.That(stream.Position, Is.Zero);
    }

    [TestCase(UploadPurpose.EmployeeDocument, "This file type is not allowed. Allowed types: PDF, Word (.docx), PNG, JPEG.")]
    [TestCase(UploadPurpose.LeaveAttachment, "This file type is not allowed. Allowed types: PDF, PNG, JPEG.")]
    [TestCase(UploadPurpose.OrganizationLogo, "This file type is not allowed. Allowed types: PNG, JPEG, WebP.")]
    public void S06_TypeNotAllowedMessage_IsTheExactTextForThePurpose(UploadPurpose purpose, string expected)
    {
        var ex = AssertRefused(purpose, "a.txt", "text/plain", "hello"u8.ToArray(), UploadRejectionReason.TypeNotAllowed);
        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Is.EqualTo(expected));
            Assert.That(ex.Code, Is.EqualTo("file_type_not_allowed"));
        });
    }

    [Test]
    public void DocumentDisplayName_IsSanitisedAndUsesTheServerExtension()
    {
        var result = Check(UploadPurpose.EmployeeDocument, "C:\\fakepath\\a\"b\u202E\r\n.PDF", "application/pdf", TestFiles.Pdf());
        Assert.That(result.DisplayFileName, Is.EqualTo("a_b.pdf"));
    }

    private CheckedUploadModel Check(UploadPurpose purpose, string? name, string? type, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return _checker.Check(purpose, stream, name, type, bytes.Length);
    }

    private UploadRejectedException AssertRefused(
        UploadPurpose purpose, string? name, string? type, byte[] bytes, UploadRejectionReason reason)
    {
        using var stream = new MemoryStream(bytes);
        var ex = Assert.Throws<UploadRejectedException>(() => _checker.Check(purpose, stream, name, type, bytes.Length));
        Assert.That(ex!.Reason, Is.EqualTo(reason), $"{name} ({type}) for {purpose}");
        return ex;
    }

    private static void AssertAccepted(CheckedUploadModel result, string ext, string type, DocumentFileKind kind, string displayName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Extension, Is.EqualTo(ext));
            Assert.That(result.ContentType, Is.EqualTo(type));
            Assert.That(result.FileKind, Is.EqualTo(kind));
            Assert.That(result.DisplayFileName, Is.EqualTo(displayName));
        });
    }

    private static TestCaseData Refused(string id, UploadPurpose purpose, string name, string? type, byte[] bytes)
        => new TestCaseData(purpose, name, type, bytes).SetName(id);
}
