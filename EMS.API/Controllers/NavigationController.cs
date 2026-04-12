using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EMS.Application.DTOs.Navigation;
using EMS.Application.Services.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NavigationController : ControllerBase
{
    private readonly INavigationService _navigation;

    public NavigationController(INavigationService navigation)
    {
        _navigation = navigation;
    }

    /// <summary>Menus the current user may access (tree), derived from roles and EMS role permissions.</summary>
    [HttpGet("menus")]
    [ProducesResponseType(typeof(IReadOnlyList<MenuResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<MenuResponseModel>>> GetMenus(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var menus = await _navigation.GetMenusForUserAsync(userId.Value, cancellationToken);
        return Ok(menus);
    }

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var id) ? id : null;
    }
}
