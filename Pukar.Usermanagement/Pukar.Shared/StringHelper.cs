using System.Text.RegularExpressions;

namespace Pukar.Shared;

public static class StringHelper
{
    private static readonly string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    /// <summary>Returns null if null/whitespace; otherwise the trimmed string.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Trims required text; throws if null or whitespace.</summary>
    public static string NormalizeRequired(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
        return value.Trim();
    }

    /// <summary>True when both are null, or both trim to the same ordinal string.</summary>
    public static bool EqualsTrimmed(string? a, string? b)
    {
        if (a is null && b is null)
            return true;
        if (a is null || b is null)
            return false;
        return string.Equals(a.Trim(), b.Trim(), StringComparison.Ordinal);
    }

    /// <summary>Ordinal, case-insensitive equality after trimming both sides.</summary>
    public static bool EqualsOrdinalIgnoreCase(string? a, string? b)
    {
        if (a is null && b is null)
            return true;
        if (a is null || b is null)
            return false;
        return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the two strings are not ordinally equal but match when ignoring case
    /// (e.g. "Acme" vs "acme"). False if either is null/empty, or if they are identical including case.
    /// </summary>
    public static bool DifferOnlyByCase(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;
        if (string.Equals(a, b, StringComparison.Ordinal))
            return false;
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public static bool AnyCharacterUppercase(this string? givenString)
    {
        if (string.IsNullOrEmpty(givenString))
            return false;
        return givenString.Any(char.IsUpper);
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            return Regex.IsMatch(email, EmailPattern, RegexOptions.IgnoreCase);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
