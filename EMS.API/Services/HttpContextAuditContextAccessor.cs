using EMS.Application.Services.Authorization;

namespace EMS.API.Services;

public sealed class HttpContextAuditContextAccessor : IAuditContextAccessor
{
    private readonly IIdentityContext _identityContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextAuditContextAccessor(IIdentityContext identityContext, IHttpContextAccessor httpContextAccessor)
    {
        _identityContext = identityContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public UserIdentitySnapshot GetCurrentUser() => _identityContext.GetCurrent();

    public string? GetCorrelationId() => _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
}
