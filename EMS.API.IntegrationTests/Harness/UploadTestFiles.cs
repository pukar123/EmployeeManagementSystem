using System.IO.Compression;
using System.Text;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// Minimal real files of the types EMS accepts (B10), built in code. A small copy of the helper in
/// EMS.Application.UnitTests, so neither test project needs to reference the other.
/// </summary>
public static class UploadTestFiles
{
    public const string DocxType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    /// <summary>A minimal PDF. <paramref name="marker"/> goes in a comment line so each test's file is distinct.</summary>
    public static byte[] Pdf(string marker = "B10") => Encoding.ASCII.GetBytes(
        $"%PDF-1.4\n% {marker}\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[]/Count 0>>endobj\n" +
        "trailer<</Root 1 0 R>>\n%%EOF\n");

    /// <summary>A 1x1 PNG.</summary>
    public static byte[] Png() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    public static byte[] Jpeg() =>
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01,
        0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9,
    ];

    public static byte[] Html() => "<!DOCTYPE html><html><body><script>alert(1)</script></body></html>"u8.ToArray();

    public static byte[] Svg() =>
        "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray();

    public static byte[] Exe() => [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00];

    /// <summary>A zip holding the two entries every Word document has.</summary>
    public static byte[] Docx()
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in new[] { "[Content_Types].xml", "word/document.xml" })
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write("<x/>");
            }
        }

        return buffer.ToArray();
    }

    /// <summary><paramref name="header"/> padded with zero bytes to exactly <paramref name="length"/> bytes.</summary>
    public static byte[] Padded(byte[] header, long length)
    {
        var bytes = new byte[length];
        header.CopyTo(bytes, 0);
        return bytes;
    }
}
