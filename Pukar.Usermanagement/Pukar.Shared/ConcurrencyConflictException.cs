namespace Pukar.Shared;

/// <summary>Optimistic concurrency failure — the resource was modified concurrently (HTTP 409).</summary>
public sealed class ConcurrencyConflictException : BusinessRuleException
{
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }
}
