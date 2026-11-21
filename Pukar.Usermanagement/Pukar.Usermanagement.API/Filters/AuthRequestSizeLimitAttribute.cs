using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Pukar.Usermanagement.API.Filters;

/// <summary>
/// Caps the request body of the anonymous auth actions at <see cref="MaxBodyBytes"/> (16 KB; the real
/// bodies are a few hundred bytes). Runs before model binding and does two things:
/// <list type="bullet">
/// <item>sets the server's maximum body size for this request, as <c>[RequestSizeLimit]</c> does, so a
/// body without a Content-Length (chunked) is cut off by Kestrel while it is read;</item>
/// <item>answers 413 straight away when the declared Content-Length is already too large, so the body
/// is never read (this also works in hosts without that server feature, such as TestServer).</item>
/// </list>
/// The 413 is the same for every caller and says nothing about any account.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AuthRequestSizeLimitAttribute : Attribute, IResourceFilter, IOrderedFilter
{
    public const long MaxBodyBytes = 16 * 1024;
    public const string Code = "request_too_large";
    public const string Message = "The request is too large.";

    // Before other resource filters, as the built-in RequestSizeLimit filter is.
    public int Order => -2000;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var httpContext = context.HttpContext;
        var maxBodySize = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (maxBodySize is { IsReadOnly: false })
            maxBodySize.MaxRequestBodySize = MaxBodyBytes;

        if (httpContext.Request.ContentLength > MaxBodyBytes)
        {
            context.Result = new ObjectResult(new { code = Code, message = Message })
            {
                StatusCode = StatusCodes.Status413PayloadTooLarge,
            };
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
