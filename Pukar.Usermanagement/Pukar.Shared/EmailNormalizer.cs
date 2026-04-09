namespace Pukar.Shared;

public static class EmailNormalizer
{
    public static string Normalize(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}
