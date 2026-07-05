namespace EMS.Application.Services.Integrations;

/// <summary>
/// Raised when User Management is unreachable or returns a dependency failure for a required operation.
/// Controllers should map this to HTTP 503 and never report identity mutations as successful.
/// </summary>
public sealed class UserManagementDependencyUnavailableException : Exception
{
    public UserManagementDependencyUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public int? StatusCode { get; init; }
}
