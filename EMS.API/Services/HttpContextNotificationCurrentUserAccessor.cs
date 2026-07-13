using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Pukar.Notifications.Application.Services;

namespace EMS.API.Services;

public sealed class HttpContextNotificationCurrentUserAccessor : INotificationCurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextNotificationCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(sub, out var parsed) ? parsed : null;
    }
}
