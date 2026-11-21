using Microsoft.AspNetCore.Http;

namespace Pukar.Usermanagement.API.RateLimiting;

/// <summary>
/// The per-IP partition key: the connection's remote address, or "unknown" when the host gives none.
/// Deliberately ignores X-Forwarded-For and similar headers: without a trusted-proxy set-up they are
/// chosen by the caller and would let anyone bypass the limit.
/// </summary>
public static class ClientIpPartitionKey
{
    public const string Unknown = "unknown";

    public static string From(HttpContext httpContext)
        => httpContext.Connection.RemoteIpAddress?.ToString() ?? Unknown;
}
