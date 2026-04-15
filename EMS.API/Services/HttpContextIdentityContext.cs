using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EMS.Application.Services.Authorization;

namespace EMS.API.Services;

public sealed class HttpContextIdentityContext : IIdentityContext
{
    private const string RolesClaimType = "roles";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextIdentityContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UserIdentitySnapshot GetCurrent()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return new UserIdentitySnapshot();

        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        int? userId = int.TryParse(sub, out var parsed) ? parsed : null;

        var roleKeys = user.FindAll(RolesClaimType)
            .Select(static c => c.Value)
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Select(static role => role.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new UserIdentitySnapshot
        {
            UserId = userId,
            Email = user.FindFirstValue(JwtRegisteredClaimNames.Email),
            UserName = user.FindFirstValue(JwtRegisteredClaimNames.Name)
                       ?? user.FindFirstValue(ClaimTypes.Name),
            RoleKeys = roleKeys,
        };
    }
}
