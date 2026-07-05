using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pukar.Usermanagement.Contracts.ServiceAuth;

namespace Pukar.Usermanagement.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ServiceAuditAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ServiceAuditAttribute>>();
        var request = context.HttpContext.Request;

        var clientId = context.HttpContext.User.FindFirst(ServiceAuthConstants.ClientIdClaimType)?.Value ?? "unknown";
        var initiatingUserId = request.Headers[AuditHeaders.InitiatingUserId].FirstOrDefault();
        var initiatingUserEmail = request.Headers[AuditHeaders.InitiatingUserEmail].FirstOrDefault();
        var correlationId = request.Headers["X-Correlation-Id"].FirstOrDefault() ?? request.HttpContext.TraceIdentifier;

        logger.LogInformation(
            "Internal API {Method} {Path} serviceClient={ClientId} initiatingUserId={InitiatingUserId} initiatingUserEmail={InitiatingUserEmail} correlationId={CorrelationId}",
            request.Method,
            request.Path,
            clientId,
            initiatingUserId,
            initiatingUserEmail,
            correlationId);

        await next();
    }
}
