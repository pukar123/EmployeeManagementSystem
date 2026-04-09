using System.Security.Cryptography;
using System.Text;

namespace Pukar.Usermanagement.Application.Helpers;

public static class RefreshTokenCrypto
{
    public static string GenerateOpaqueToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
