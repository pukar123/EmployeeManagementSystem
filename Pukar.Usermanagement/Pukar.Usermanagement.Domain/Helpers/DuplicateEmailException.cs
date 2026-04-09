namespace Pukar.Usermanagement.Domain.Helpers;

public class DuplicateEmailException : BusinessRuleException
{
    public DuplicateEmailException()
        : base("This email is already registered.")
    {
    }
}
