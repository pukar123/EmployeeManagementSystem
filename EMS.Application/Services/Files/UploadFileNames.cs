using System.Globalization;
using System.Text;

namespace EMS.Application.Services.Files;

/// <summary>
/// Cleans user file names (B10). The original name is display metadata only: path parts, control and format
/// (bidi, zero-width) characters and characters that are unsafe in file names or headers are removed, the length
/// is capped, and the server's extension is used. Also used for download names, so rows saved before B10 are
/// cleaned when they are served.
/// </summary>
public static class UploadFileNames
{
    /// <summary>Longest base name kept, in <see cref="char"/>s (before the extension).</summary>
    public const int MaxBaseLength = 120;

    private static readonly char[] ReplacedCharacters = ['"', '*', ':', '<', '>', '?', '|', '/', '\\'];

    /// <summary>
    /// Returns the sanitised base name of <paramref name="name"/> followed by <paramref name="serverExtension"/>.
    /// Uses <paramref name="fallbackBase"/> when nothing is left. Idempotent.
    /// </summary>
    public static string SanitiseDisplayName(string? name, string serverExtension, string fallbackBase)
    {
        var cleaned = TrimSpacesAndDots(CleanName(name));
        var baseName = TrimSpacesAndDots(RemoveExtension(cleaned));

        if (baseName.Length > MaxBaseLength)
        {
            var cut = MaxBaseLength;
            if (char.IsHighSurrogate(baseName[cut - 1]))
                cut--;
            baseName = TrimSpacesAndDots(baseName[..cut]);
        }

        if (baseName.Length == 0)
            baseName = fallbackBase;

        return baseName + (serverExtension ?? string.Empty).ToLowerInvariant();
    }

    /// <summary>
    /// The last extension of the cleaned name, lower case with the dot (<c>report.exe.pdf</c> gives <c>.pdf</c>,
    /// <c>report.pdf.exe</c> gives <c>.exe</c>), or an empty string when there is none.
    /// </summary>
    public static string ExtensionOf(string? fileName)
    {
        var cleaned = TrimSpacesAndDots(CleanName(fileName));
        var dot = cleaned.LastIndexOf('.');
        return dot < 0 ? string.Empty : cleaned[dot..].ToLowerInvariant();
    }

    /// <summary>
    /// The extension of a stored key when it is a plain <c>.xxx</c> or <c>.xxxx</c> of ASCII letters and digits
    /// (lower case), otherwise an empty string. Used to clean names of rows saved before B10.
    /// </summary>
    public static string ServerExtensionOfKey(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return string.Empty;

        var lastSegment = storageKey.Replace('\\', '/');
        lastSegment = lastSegment[(lastSegment.LastIndexOf('/') + 1)..];
        var dot = lastSegment.LastIndexOf('.');
        if (dot < 0)
            return string.Empty;

        var ext = lastSegment[(dot + 1)..];
        if (ext.Length is < 3 or > 4 || !ext.All(char.IsAsciiLetterOrDigit))
            return string.Empty;

        return "." + ext.ToLowerInvariant();
    }

    /// <summary>
    /// Steps 1 to 4 of the B10 rules: keep only the part after the last <c>/</c> or <c>\</c>, normalise to NFC,
    /// remove control (Cc), format (Cf) and lone surrogate characters, and replace unsafe characters with <c>_</c>.
    /// </summary>
    private static string CleanName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        var value = name.Replace('\\', '/');
        value = value[(value.LastIndexOf('/') + 1)..];
        value = RemoveLoneSurrogates(value).Normalize(NormalizationForm.FormC);

        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            var category = char.GetUnicodeCategory(c);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format)
                continue;

            builder.Append(Array.IndexOf(ReplacedCharacters, c) >= 0 ? '_' : c);
        }

        // Normalised again: removing a format character can leave a sequence that NFC would combine.
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Removes a trailing extension of 1 to 10 letters or digits (<c>a.PDF</c> gives <c>a</c>).</summary>
    private static string RemoveExtension(string value)
    {
        var dot = value.LastIndexOf('.');
        if (dot < 0)
            return value;

        var ext = value[(dot + 1)..];
        return ext.Length is >= 1 and <= 10 && ext.All(char.IsLetterOrDigit) ? value[..dot] : value;
    }

    private static string TrimSpacesAndDots(string value) => value.Trim(' ', '.');

    /// <summary>Drops surrogate characters that are not part of a valid pair, so NFC normalisation cannot throw.</summary>
    private static string RemoveLoneSurrogates(string value)
    {
        StringBuilder? builder = null;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var valid = !char.IsSurrogate(c)
                        || (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]));
            if (char.IsLowSurrogate(c) && i > 0 && char.IsHighSurrogate(value[i - 1]))
                valid = true;

            if (!valid)
            {
                builder ??= new StringBuilder(value[..i]);
                continue;
            }

            builder?.Append(c);
        }

        return builder?.ToString() ?? value;
    }
}
