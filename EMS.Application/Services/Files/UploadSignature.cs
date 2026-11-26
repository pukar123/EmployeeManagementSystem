namespace EMS.Application.Services.Files;

/// <summary>Content signature (magic bytes) an allowed upload type must have.</summary>
public enum UploadSignature
{
    /// <summary>Bytes 0 to 4 are <c>%PDF-</c>.</summary>
    Pdf = 1,

    /// <summary>Bytes 0 to 7 are <c>89 50 4E 47 0D 0A 1A 0A</c>.</summary>
    Png = 2,

    /// <summary>Bytes 0 to 2 are <c>FF D8 FF</c>.</summary>
    Jpeg = 3,

    /// <summary>Bytes 0 to 3 are <c>RIFF</c> and bytes 8 to 11 are <c>WEBP</c>.</summary>
    WebP = 4,

    /// <summary>Zip signature <c>50 4B 03 04</c> plus the Word entry rule (see <see cref="UploadFileChecker"/>).</summary>
    Docx = 5,
}
