using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pukar.Shared;
using Pukar.Usermanagement.API.RateLimiting;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Contracts.Auth;

namespace Pukar.Usermanagement.API.Filters;

/// <summary>
/// Per-email rate limit for sign-in and forgot-password. Runs after model binding (the rate limiter
/// middleware cannot read the body), normalises the bound <c>Email</c> and takes a permit from
/// <see cref="AuthEmailRateLimiter"/>. A blank email is not counted here (the per-IP limit still
/// applies and the service returns its normal answer). A rejection writes the shared 429 response.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EmailRateLimitAttribute : Attribute, IAsyncActionFilter
{
    public EmailRateLimitAttribute(string policyName)
    {
        PolicyName = policyName;
    }

    public string PolicyName { get; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var settings = services.GetRequiredService<IOptions<AuthRateLimitingOptions>>().Value;
        var email = FindEmail(context.ActionArguments.Values);

        if (!settings.Enabled || string.IsNullOrWhiteSpace(email))
        {
            await next();
            return;
        }

        var limiter = services.GetRequiredService<AuthEmailRateLimiter>();
        using var lease = limiter.AttemptAcquire(PolicyName, EmailNormalizer.Normalize(email));
        if (!lease.IsAcquired)
        {
            await AuthRateLimitRejectionWriter.WriteAsync(
                context.HttpContext,
                lease,
                limiter.WindowFor(PolicyName),
                PolicyName,
                context.HttpContext.RequestAborted);
            context.Result = new EmptyResult();
            return;
        }

        await next();
    }

    private static string? FindEmail(IEnumerable<object?> arguments)
    {
        foreach (var argument in arguments)
        {
            switch (argument)
            {
                case LoginRequestModel login:
                    return login.Email;
                case ForgotPasswordRequestModel forgot:
                    return forgot.Email;
            }
        }

        return null;
    }
}
