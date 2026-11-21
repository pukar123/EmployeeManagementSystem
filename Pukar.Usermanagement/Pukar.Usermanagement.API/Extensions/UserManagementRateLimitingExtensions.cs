using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using Pukar.Usermanagement.API.RateLimiting;
using Pukar.Usermanagement.Application.Options;

namespace Pukar.Usermanagement.API.Extensions;

/// <summary>
/// Rate limiting for the anonymous auth endpoints, shared by every host that serves the UM controllers
/// (EMS.API in-process and the standalone UM host). There is no global limiter: only actions marked
/// with <c>[EnableRateLimiting]</c> or <see cref="Filters.EmailRateLimitAttribute"/> are counted.
/// </summary>
public static class UserManagementRateLimitingExtensions
{
    public static IServiceCollection AddPukarUserManagementRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AuthRateLimitingOptions>()
            .Bind(configuration.GetSection(AuthRateLimitingOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AuthRateLimitingOptions>, AuthRateLimitingOptionsValidator>());

        services.TryAddSingleton<AuthEmailRateLimiter>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejectedAsync;

            foreach (var policyName in AuthRateLimitPolicies.PerIpPolicies)
                options.AddPolicy(policyName, httpContext => PerIpPartition(policyName, httpContext));
        });

        return services;
    }

    /// <summary>
    /// Adds the rate limiter middleware. Call it after <c>UseCors</c> (so 429s carry CORS headers and
    /// preflight requests are answered before anything is counted) and before <c>UseAuthentication</c>.
    /// </summary>
    public static IApplicationBuilder UsePukarUserManagementRateLimiting(this IApplicationBuilder app)
        => app.UseRateLimiter();

    private static RateLimitPartition<string> PerIpPartition(string policyName, HttpContext httpContext)
    {
        var settings = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitingOptions>>().Value;
        if (!settings.Enabled)
            return RateLimitPartition.GetNoLimiter(string.Empty);

        var limit = AuthRateLimitPolicies.LimitFor(policyName, settings);
        return RateLimitPartition.GetFixedWindowLimiter(
            ClientIpPartitionKey.From(httpContext),
            _ => AuthEmailRateLimiter.ToLimiterOptions(limit));
    }

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var policyName = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
                         ?? "unknown";
        var settings = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitingOptions>>().Value;
        var fallbackWindow = AuthRateLimitPolicies.PerIpPolicies.Contains(policyName)
            ? AuthRateLimitPolicies.LimitFor(policyName, settings).Window
            : TimeSpan.FromSeconds(60);

        await AuthRateLimitRejectionWriter.WriteAsync(
            httpContext, context.Lease, fallbackWindow, policyName, cancellationToken);
    }
}
