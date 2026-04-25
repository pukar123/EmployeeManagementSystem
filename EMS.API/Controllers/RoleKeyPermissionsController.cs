using EMS.Application.DTOs.Authorization;
using EMS.Application.Services.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RoleKeyPermissionsController : ControllerBase
{
    private readonly IRoleKeyPermissionService _roleKeyPermissions;

    public RoleKeyPermissionsController(IRoleKeyPermissionService roleKeyPermissions)
    {
        _roleKeyPermissions = roleKeyPermissions;
    }

    [HttpGet("{roleKey}")]
    [Authorize(Policy = "AdminAccess")]
    [ProducesResponseType(typeof(RoleMenuAccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleMenuAccessResponseModel>> GetByRoleKey(string roleKey, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roleKeyPermissions.GetAsync(roleKey, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{roleKey}")]
    [Authorize(Policy = "AdminAccess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetForRoleKey(
        string roleKey,
        [FromBody] SetRoleMenuAccessRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _roleKeyPermissions.SetAsync(roleKey, request, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
