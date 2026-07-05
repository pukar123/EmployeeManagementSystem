namespace Pukar.Shared;

/// <summary>Invalid service-client credentials (HTTP 401).</summary>
public sealed class UnauthorizedServiceException : Exception
{
    public UnauthorizedServiceException(string message)
        : base(message)
    {
    }
}
