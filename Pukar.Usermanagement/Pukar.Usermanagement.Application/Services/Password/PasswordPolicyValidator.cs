using Microsoft.Extensions.Options;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Options;

namespace Pukar.Usermanagement.Application.Services.Password;

public sealed class PasswordPolicyValidator : IPasswordPolicyValidator
{
    private readonly PasswordPolicyOptions _options;

    public PasswordPolicyValidator(IOptions<PasswordPolicyOptions> options)
    {
        _options = options.Value;
    }

    public void ValidateOrThrow(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new BusinessRuleException("Password is required.");

        if (password.Length < _options.MinimumLength)
            throw new BusinessRuleException($"Password must be at least {_options.MinimumLength} characters.");

        if (_options.RequireUppercase && !password.Any(char.IsUpper))
            throw new BusinessRuleException("Password must contain at least one uppercase letter.");

        if (_options.RequireLowercase && !password.Any(char.IsLower))
            throw new BusinessRuleException("Password must contain at least one lowercase letter.");

        if (_options.RequireDigit && !password.Any(char.IsDigit))
            throw new BusinessRuleException("Password must contain at least one digit.");

        if (_options.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            throw new BusinessRuleException("Password must contain at least one non-alphanumeric character.");
    }
}
