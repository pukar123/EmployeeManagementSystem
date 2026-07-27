using Pukar.Usermanagement.Application.Services.Password;

namespace Pukar.Usermanagement.Infrastructure.Services;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A malformed or legacy stored hash must not turn an authentication
            // failure into a server error.
            return false;
        }
    }
}
