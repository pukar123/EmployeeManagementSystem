namespace Pukar.Usermanagement.Application.Services.Password;

public interface IPasswordPolicyValidator
{
    void ValidateOrThrow(string password);
}
