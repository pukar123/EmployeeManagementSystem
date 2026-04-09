namespace Pukar.Shared;

public class DuplicateEmailException : BusinessRuleException
{
    public DuplicateEmailException()
        : base("This email is already registered.")
    {
    }
}
