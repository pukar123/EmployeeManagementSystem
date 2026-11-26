using System.Buffers.Binary;
using System.IO.Compression;
using EMS.Application.DTOs.Files;
using EMS.Domain.Enums;

namespace EMS.Application.Services.Files;

/// <summary>
/// Pure upload checks (B10), with no I/O beyond reading the stream it is given. Order: empty, too large, extension,
/// declared content type, content signature. Signatures read at most the first 16 bytes; a <c>.docx</c> also has its
/// zip entry list read (never extracted). Any failure is an <see cref="UploadRejectedException"/>, never a 500.
/// Stateless, so it is registered as a singleton.
/// </summary>
public sealed class UploadFileChecker : IUploadFileChecker
{
    /// <summary>A Word document with more entries than this is refused (a real one has a few dozen).</summary>
    public const int MaxZipEntries = 10_000;

    private const int SignatureBytes = 16;
    private const string MacroEntryName = "word/vbaProject.bin";
    private const int EndOfCentralDirectorySize = 22;
    private const int MaxZipCommentLength = ushort.MaxValue;
    private const int Zip64LocatorSize = 20;
    private const uint EndOfCentralDirectorySignature = 0x06054B50;
    private const uint Zip64LocatorSignature = 0x07064B50;

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> ZipSignature => [0x50, 0x4B, 0x03, 0x04];

    public CheckedUploadModel Check(UploadPurpose purpose, Stream content, string? fileName, string? declaredContentType, long length)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanSeek || !content.CanRead)
            throw new ArgumentException("The upload stream must be readable and seekable.", nameof(content));

        var rule = UploadRules.For(purpose);

        if (length <= 0)
            throw new UploadRejectedException(purpose, UploadRejectionReason.Empty, UploadRejectedException.EmptyMessage, "size");
        if (length > rule.MaxBytes)
            throw new UploadRejectedException(purpose, UploadRejectionReason.TooLarge, rule.TooLargeMessage, "size");

        var extension = UploadFileNames.ExtensionOf(fileName);
        var type = rule.AllowedTypes.FirstOrDefault(t => t.Extensions.Contains(extension, StringComparer.Ordinal))
                   ?? throw NotAllowed(rule, "extension");

        var declared = NormaliseContentType(declaredContentType);
        if (!type.ContentTypes.Contains(declared, StringComparer.Ordinal))
            throw NotAllowed(rule, "declared content type");

        bool signatureMatches;
        try
        {
            signatureMatches = MatchesSignature(type.Signature, content);
        }
        catch (Exception)
        {
            // A corrupt, truncated or hostile file (for example a broken zip directory) is a refusal, never a 500.
            signatureMatches = false;
        }
        finally
        {
            content.Position = 0;
        }

        if (!signatureMatches)
            throw NotAllowed(rule, "signature");

        return new CheckedUploadModel
        {
            Extension = type.ServerExtension,
            ContentType = type.CanonicalContentType,
            DisplayFileName = UploadFileNames.SanitiseDisplayName(fileName, type.ServerExtension, rule.FallbackBaseName),
            FileKind = purpose == UploadPurpose.OrganizationLogo ? DocumentFileKind.None : type.FileKind,
            SizeBytes = length,
        };
    }

    /// <summary>Lower-case media type without parameters (<c>Application/PDF; charset=binary</c> gives <c>application/pdf</c>).</summary>
    private static string NormaliseContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return string.Empty;

        var semicolon = contentType.IndexOf(';');
        var mediaType = semicolon < 0 ? contentType : contentType[..semicolon];
        return mediaType.Trim().ToLowerInvariant();
    }

    private static UploadRejectedException NotAllowed(UploadRule rule, string failedCheck)
        => new(rule.Purpose, UploadRejectionReason.TypeNotAllowed, rule.TypeNotAllowedMessage, failedCheck);

    private static bool MatchesSignature(UploadSignature signature, Stream content)
    {
        content.Position = 0;
        var header = new byte[SignatureBytes];
        var read = content.ReadAtLeast(header, SignatureBytes, throwOnEndOfStream: false);
        ReadOnlySpan<byte> bytes = header.AsSpan(0, read);

        return signature switch
        {
            UploadSignature.Pdf => bytes.StartsWith("%PDF-"u8),
            UploadSignature.Png => bytes.StartsWith(PngSignature),
            UploadSignature.Jpeg => bytes.StartsWith(JpegSignature),
            UploadSignature.WebP => bytes.Length >= 12 && bytes.StartsWith("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
            UploadSignature.Docx => bytes.StartsWith(ZipSignature) && IsWordDocument(content),
            _ => false,
        };
    }

    /// <summary>
    /// Checks the zip's End Of Central Directory record first (cheap: at most the last 64 KB), then opens the zip in
    /// read mode and reads only its entry list (nothing is decompressed). It must hold <c>[Content_Types].xml</c> and
    /// <c>word/document.xml</c>, and must not hold <c>word/vbaProject.bin</c> in any letter case (macros).
    /// </summary>
    private static bool IsWordDocument(Stream content)
    {
        // Refused before ZipArchive reads the central directory, so a zip with a huge entry list costs no memory.
        if (!HasAcceptableEntryCount(content))
            return false;

        content.Position = 0;
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        var entries = archive.Entries;
        if (entries.Count > MaxZipEntries)
            return false;

        var hasContentTypes = false;
        var hasDocument = false;
        foreach (var entry in entries)
        {
            if (string.Equals(entry.FullName, MacroEntryName, StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(entry.FullName, "[Content_Types].xml", StringComparison.Ordinal))
                hasContentTypes = true;
            else if (string.Equals(entry.FullName, "word/document.xml", StringComparison.Ordinal))
                hasDocument = true;
        }

        return hasContentTypes && hasDocument;
    }

    /// <summary>
    /// Finds the End Of Central Directory record (signature <c>50 4B 05 06</c>) by searching back from the end of the
    /// stream over at most 22 bytes plus the largest zip comment (65,535 bytes), and reads its total entry count.
    /// False when there is no record, the zip spans several disks, it is ZIP64 (a ZIP64 locator before the record,
    /// or the 0xFFFF / 0xFFFFFFFF markers; a real <c>.docx</c> never needs ZIP64), or it has more than
    /// <see cref="MaxZipEntries"/> entries. Leaves the stream at position 0.
    /// </summary>
    private static bool HasAcceptableEntryCount(Stream content)
    {
        try
        {
            var length = content.Length;
            if (length < EndOfCentralDirectorySize)
                return false;

            var tailLength = (int)Math.Min(length, EndOfCentralDirectorySize + MaxZipCommentLength + Zip64LocatorSize);
            var tail = new byte[tailLength];
            content.Position = length - tailLength;
            if (content.ReadAtLeast(tail, tailLength, throwOnEndOfStream: false) != tailLength)
                return false;

            var recordAt = -1;
            var lowest = Math.Max(0, tailLength - EndOfCentralDirectorySize - MaxZipCommentLength);
            for (var i = tailLength - EndOfCentralDirectorySize; i >= lowest; i--)
            {
                if (BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i)) == EndOfCentralDirectorySignature)
                {
                    recordAt = i;
                    break;
                }
            }

            if (recordAt < 0)
                return false;

            var record = tail.AsSpan(recordAt, EndOfCentralDirectorySize);
            var diskNumber = BinaryPrimitives.ReadUInt16LittleEndian(record[4..]);
            var directoryDisk = BinaryPrimitives.ReadUInt16LittleEndian(record[6..]);
            var entriesOnDisk = BinaryPrimitives.ReadUInt16LittleEndian(record[8..]);
            var totalEntries = BinaryPrimitives.ReadUInt16LittleEndian(record[10..]);
            var directorySize = BinaryPrimitives.ReadUInt32LittleEndian(record[12..]);
            var directoryOffset = BinaryPrimitives.ReadUInt32LittleEndian(record[16..]);

            if (diskNumber != 0 || directoryDisk != 0 || entriesOnDisk != totalEntries)
                return false;

            // ZIP64 is refused: its markers in the classic record, or a ZIP64 locator just before it.
            if (totalEntries == ushort.MaxValue || directorySize == uint.MaxValue || directoryOffset == uint.MaxValue)
                return false;
            if (recordAt >= Zip64LocatorSize &&
                BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(recordAt - Zip64LocatorSize)) == Zip64LocatorSignature)
                return false;

            return totalEntries <= MaxZipEntries;
        }
        finally
        {
            content.Position = 0;
        }
    }
}
