namespace Pukar.Usermanagement.Application.Services.Password;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
