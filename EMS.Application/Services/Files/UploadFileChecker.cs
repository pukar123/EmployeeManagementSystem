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
    /// Opens the zip in read mode and reads only its entry list (nothing is decompressed). It must hold
    /// <c>[Content_Types].xml</c> and <c>word/document.xml</c>, and must not hold <c>word/vbaProject.bin</c> (macros).
    /// </summary>
    private static bool IsWordDocument(Stream content)
    {
        content.Position = 0;
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        var entries = archive.Entries;
        if (entries.Count > MaxZipEntries)
            return false;

        var hasContentTypes = false;
        var hasDocument = false;
        foreach (var entry in entries)
        {
            switch (entry.FullName)
            {
                case "[Content_Types].xml":
                    hasContentTypes = true;
                    break;
                case "word/document.xml":
                    hasDocument = true;
                    break;
                case "word/vbaProject.bin":
                    return false;
            }
        }

        return hasContentTypes && hasDocument;
    }
}
