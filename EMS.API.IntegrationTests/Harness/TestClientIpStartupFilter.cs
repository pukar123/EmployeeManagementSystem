using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// Test harness only. TestServer gives every request a null client IP, so all requests would share one
/// rate-limit bucket. This filter runs before the API's own middleware and, when a request carries
/// <see cref="HeaderName"/>, sets <c>Connection.RemoteIpAddress</c> from it. It is registered only by
/// <see cref="EmsApiFactory"/>; the production API never reads this header (or X-Forwarded-For).
/// </summary>
internal sealed class TestClientIpStartupFilter : IStartupFilter
{
    public const string HeaderName = "X-Test-Client-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(HeaderName, out var value)
                    && IPAddress.TryParse(value.ToString(), out var address))
                {
                    context.Connection.RemoteIpAddress = address;
                }

                await nextMiddleware(context);
            });

            next(app);
        };
}
