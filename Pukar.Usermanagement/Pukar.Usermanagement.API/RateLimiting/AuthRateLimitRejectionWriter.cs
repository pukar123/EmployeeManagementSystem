using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pukar.Usermanagement.API.RateLimiting;

/// <summary>
/// Writes the one 429 response used by every auth limit (middleware and per-email filter alike):
/// a whole-second <c>Retry-After</c> header and <c>{ code, message, retryAfterSeconds }</c>.
/// The response is the same whether or not the account exists.
/// </summary>
public static class AuthRateLimitRejectionWriter
{
    public const string Code = "rate_limited";
    public const string Message = "Too many attempts. Please wait a moment and try again.";

    private const string LoggerCategory = "Pukar.Usermanagement.RateLimiting";

    public static async Task WriteAsync(
        HttpContext httpContext,
        RateLimitLease lease,
        TimeSpan fallbackWindow,
        string policyName,
        CancellationToken cancellationToken)
    {
        var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var fromLease) ? fromLease : fallbackWindow;
        var retryAfterSeconds = ToWholeSeconds(retryAfter);

        // Policy and path only: never the email, password or token from the request.
        httpContext.RequestServices.GetService<ILoggerFactory>()?
            .CreateLogger(LoggerCategory)
            .LogWarning(
                "Auth rate limit reached: policy {Policy} path {Path}; retry after {RetryAfterSeconds}s",
                policyName,
                httpContext.Request.Path.Value,
                retryAfterSeconds);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        await httpContext.Response.WriteAsJsonAsync(
            new { code = Code, message = Message, retryAfterSeconds },
            cancellationToken);
    }

    /// <summary>Rounds up to whole seconds, at least 1.</summary>
    public static int ToWholeSeconds(TimeSpan retryAfter)
        => Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
}
