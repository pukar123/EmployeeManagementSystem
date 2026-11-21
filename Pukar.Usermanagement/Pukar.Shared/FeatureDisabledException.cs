namespace Pukar.Shared;

/// <summary>
/// Thrown when a caller uses a feature that is turned off by configuration (for example
/// self-registration). Hosts map it to 404 so a disabled feature looks like a missing endpoint.
/// </summary>
public class FeatureDisabledException : Exception
{
    public FeatureDisabledException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Stable machine-readable code returned to the caller, e.g. <c>registration_disabled</c>.</summary>
    public string Code { get; }
}
