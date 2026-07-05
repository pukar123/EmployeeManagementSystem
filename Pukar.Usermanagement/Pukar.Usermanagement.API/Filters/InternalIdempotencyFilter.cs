using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Pukar.Shared;
using Pukar.Usermanagement.Contracts.ServiceAuth;
using Pukar.Usermanagement.Domain.Repositories.Interface;

namespace Pukar.Usermanagement.API.Filters;

/// <summary>
/// Enforces server-side idempotency for mutating internal service endpoints via Idempotency-Key header.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class InternalIdempotencyAttribute : TypeFilterAttribute
{
    public InternalIdempotencyAttribute() : base(typeof(InternalIdempotencyFilter))
    {
    }
}

public sealed class InternalIdempotencyFilter : IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    private readonly IIdempotencyRecordRepository _records;

    public InternalIdempotencyFilter(IIdempotencyRecordRepository records)
    {
        _records = records;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!HttpMethods.IsPost(http.Request.Method)
            && !HttpMethods.IsPut(http.Request.Method)
            && !HttpMethods.IsPatch(http.Request.Method)
            && !HttpMethods.IsDelete(http.Request.Method))
        {
            await next();
            return;
        }

        if (!http.Request.Headers.TryGetValue(HeaderName, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            context.Result = new BadRequestObjectResult(new { message = $"{HeaderName} header is required." });
            return;
        }

        var clientId = http.User.FindFirst(ServiceAuthConstants.ClientIdClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Service client identity is required." });
            return;
        }

        var idempotencyKey = keyValues.ToString().Trim();
        var route = $"{http.Request.Method} {http.Request.Path.Value?.TrimEnd('/') ?? "/"}";
        var fingerprint = ComputeFingerprint(http, context);

        IdempotencyLookupResult? existing;
        try
        {
            existing = await _records.TryGetAsync(clientId, route, idempotencyKey, http.RequestAborted);
        }
        catch (ConflictBusinessRuleException ex)
        {
            context.Result = new ConflictObjectResult(new { message = ex.Message });
            return;
        }

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            {
                context.Result = new ConflictObjectResult(new { message = "Idempotency-Key was already used with a different request payload." });
                return;
            }

            if (existing.IsComplete)
            {
                context.Result = BuildStoredResult(existing);
                return;
            }

            context.Result = new StatusCodeResult(StatusCodes.Status409Conflict);
            return;
        }

        IdempotencyClaimResult claim;
        try
        {
            claim = await _records.TryClaimAsync(
                clientId,
                route,
                idempotencyKey,
                fingerprint,
                DateTime.UtcNow.Add(DefaultTtl),
                http.RequestAborted);
        }
        catch (ConflictBusinessRuleException ex)
        {
            context.Result = new ConflictObjectResult(new { message = ex.Message });
            return;
        }

        if (!claim.IsOwner)
        {
            if (claim.Existing is { IsComplete: true })
            {
                context.Result = BuildStoredResult(claim.Existing);
                return;
            }

            context.Result = new StatusCodeResult(StatusCodes.Status409Conflict);
            return;
        }

        var executed = await next();

        if (executed.Result is ObjectResult or StatusCodeResult or EmptyResult)
        {
            var (statusCode, body, contentType) = ExtractResponse(executed.Result);
            await _records.CompleteAsync(claim.RecordId, statusCode, body, contentType, http.RequestAborted);
        }
        else if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            await _records.CompleteAsync(
                claim.RecordId,
                StatusCodes.Status500InternalServerError,
                "{\"message\":\"Internal server error\"}",
                "application/json",
                http.RequestAborted);
        }
    }

    private static IActionResult BuildStoredResult(IdempotencyLookupResult stored)
    {
        if (!string.IsNullOrWhiteSpace(stored.ResponseBody))
        {
            return new ContentResult
            {
                StatusCode = stored.StatusCode,
                Content = stored.ResponseBody,
                ContentType = stored.ContentType ?? "application/json",
            };
        }

        return new StatusCodeResult(stored.StatusCode);
    }

    private static (int StatusCode, string? Body, string? ContentType) ExtractResponse(IActionResult result)
        => result switch
        {
            ObjectResult obj => (
                obj.StatusCode ?? StatusCodes.Status200OK,
                obj.Value is null ? null : System.Text.Json.JsonSerializer.Serialize(obj.Value),
                "application/json"),
            StatusCodeResult code => (code.StatusCode, null, null),
            EmptyResult => (StatusCodes.Status204NoContent, null, null),
            _ => (StatusCodes.Status200OK, null, null),
        };

    private static string ComputeFingerprint(HttpContext http, ActionExecutingContext context)
    {
        var payload = context.ActionArguments
            .Where(kvp => kvp.Value is not CancellationToken)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var argsJson = JsonSerializer.Serialize(payload);
        var canonical = $"{http.Request.Method}|{http.Request.Path}|{argsJson}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash);
    }
}
