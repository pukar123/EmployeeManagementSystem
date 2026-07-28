namespace Pukar.Shared;

/// <summary>Business rule violation where the request conflicts with existing state (HTTP 409).</summary>
public sealed class ConflictBusinessRuleException : BusinessRuleException
{
    public ConflictBusinessRuleException(string message)
        : base(message)
    {
    }
}
