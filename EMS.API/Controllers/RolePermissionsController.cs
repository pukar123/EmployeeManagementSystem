using EMS.Application.DTOs.Navigation;
using EMS.Application.Services.RolePermissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RolePermissionsController : ControllerBase
{
    private readonly IRolePermissionService _rolePermissions;

    public RolePermissionsController(IRolePermissionService rolePermissions)
    {
        _rolePermissions = rolePermissions;
    }

    [HttpGet("for-role/{roleId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<RolePermissionItemModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RolePermissionItemModel>>> GetForRole(
        int roleId,
        CancellationToken cancellationToken)
    {
        var items = await _rolePermissions.GetForRoleAsync(roleId, cancellationToken);
        return Ok(items);
    }

    [HttpPut("for-role/{roleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetForRole(
        int roleId,
        [FromBody] SetRolePermissionsRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _rolePermissions.SetForRoleAsync(roleId, request, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
