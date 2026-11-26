using EMS.Application.DTOs.Files;
using EMS.Application.Services.Files;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EMS.API.Filters;

/// <summary>
/// Caps the request body of an upload action (B10) at the purpose's file limit plus 1 MB for form fields and
/// multipart overhead (documents and leave 16 MB, logo 6 MB; see <see cref="UploadRule.MaxRequestBodyBytes"/>).
/// Modelled on B05's auth size filter and replaces <c>[RequestSizeLimit]</c>. Runs before model binding and:
/// <list type="bullet">
/// <item>sets the server's maximum body size for this request, so Kestrel cuts off a larger body while it is read;</item>
/// <item>answers 413 <c>request_too_large</c> straight away when the declared Content-Length is already over the
/// cap, so the body is never read.</item>
/// </list>
/// A chunked body without a Content-Length that overruns the cap is still cut off by Kestrel, but MVC may then turn
/// that into a model-state 400. Browsers always send a Content-Length for <c>FormData</c>, so users see the 413.
/// Authorisation filters run before resource filters, so a refused caller still gets 401/403 first.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UploadRequestSizeLimitAttribute : Attribute, IResourceFilter, IOrderedFilter
{
    public const string Code = "request_too_large";

    public UploadRequestSizeLimitAttribute(UploadPurpose purpose)
    {
        Purpose = purpose;
    }

    public UploadPurpose Purpose { get; }

    // Before other resource filters, as the built-in RequestSizeLimit filter is.
    public int Order => -2000;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var rule = UploadRules.For(Purpose);
        var httpContext = context.HttpContext;

        var maxBodySize = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (maxBodySize is { IsReadOnly: false })
            maxBodySize.MaxRequestBodySize = rule.MaxRequestBodyBytes;

        if (httpContext.Request.ContentLength > rule.MaxRequestBodyBytes)
        {
            context.Result = new ObjectResult(new { code = Code, message = rule.TooLargeMessage })
            {
                StatusCode = StatusCodes.Status413PayloadTooLarge,
            };
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
