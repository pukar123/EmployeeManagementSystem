using System.IO.Compression;
using System.Text;

namespace EMS.Application.UnitTests.Files;

/// <summary>Minimal real files for upload checks, built in code (B10).</summary>
internal static class TestFiles
{
    public const string DocxType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public static byte[] Pdf() => Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[]/Count 0>>endobj\n" +
        "trailer<</Root 1 0 R>>\n%%EOF\n");

    /// <summary>A 1x1 PNG.</summary>
    public static byte[] Png() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    public static byte[] Jpeg() =>
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01,
        0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9,
    ];

    public static byte[] WebP() =>
    [
        .. "RIFF"u8.ToArray(), 0x1A, 0x00, 0x00, 0x00, .. "WEBPVP8L"u8.ToArray(),
        0x0D, 0x00, 0x00, 0x00, 0x2F, 0x00, 0x00, 0x00, 0x10, 0x07, 0x10, 0x11, 0x11, 0x88, 0x88, 0xFE, 0x07, 0x00,
    ];

    public static byte[] Gif() => "GIF89a\u0001\u0000\u0001\u0000\u0000\u0000\u0000;"u8.ToArray();

    public static byte[] Html() => "<!DOCTYPE html><html><body><script>alert(1)</script></body></html>"u8.ToArray();

    public static byte[] Svg() =>
        "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray();

    public static byte[] Exe() => [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00];

    /// <summary>Legacy Word / OLE2 compound file header.</summary>
    public static byte[] Ole2() => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    public static byte[] Docx(params string[] extraEntries)
        => Zip(["[Content_Types].xml", "word/document.xml", .. extraEntries]);

    public static byte[] Zip(params string[] entries)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in entries)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write("<x/>");
            }
        }

        return buffer.ToArray();
    }

    /// <summary>A docx (the two Word entries) padded with empty entries to <paramref name="totalEntries"/> in all.</summary>
    public static byte[] DocxWithEntryCount(int totalEntries)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("[Content_Types].xml");
            archive.CreateEntry("word/document.xml");
            for (var i = 2; i < totalEntries; i++)
                archive.CreateEntry($"p/{i}");
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// <paramref name="zip"/> with a ZIP64 End Of Central Directory locator (<c>50 4B 06 07</c>) inserted just before
    /// its End Of Central Directory record. The central directory offset stays valid, as it comes earlier.
    /// </summary>
    public static byte[] WithZip64Locator(byte[] zip)
    {
        var recordAt = EndOfCentralDirectoryOffset(zip);
        byte[] locator = [0x50, 0x4B, 0x06, 0x07, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0];
        return [.. zip[..recordAt], .. locator, .. zip[recordAt..]];
    }

    /// <summary><paramref name="zip"/> with the End Of Central Directory total entry count set to <paramref name="count"/>.</summary>
    public static byte[] WithDeclaredEntryCount(byte[] zip, ushort count)
    {
        var copy = (byte[])zip.Clone();
        var recordAt = EndOfCentralDirectoryOffset(copy);
        BitConverter.TryWriteBytes(copy.AsSpan(recordAt + 8, 2), count);
        BitConverter.TryWriteBytes(copy.AsSpan(recordAt + 10, 2), count);
        return copy;
    }

    private static int EndOfCentralDirectoryOffset(byte[] zip)
    {
        for (var i = zip.Length - 22; i >= 0; i--)
        {
            if (zip[i] == 0x50 && zip[i + 1] == 0x4B && zip[i + 2] == 0x05 && zip[i + 3] == 0x06)
                return i;
        }

        throw new InvalidOperationException("Not a zip.");
    }

    /// <summary>Zip local-file signature followed by bytes that are not a zip.</summary>
    public static byte[] BrokenZip() => [0x50, 0x4B, 0x03, 0x04, .. Enumerable.Repeat((byte)0x41, 200)];

    /// <summary><paramref name="header"/> padded with zero bytes to exactly <paramref name="length"/> bytes.</summary>
    public static byte[] Padded(byte[] header, long length)
    {
        var bytes = new byte[length];
        header.CopyTo(bytes, 0);
        return bytes;
    }
}
