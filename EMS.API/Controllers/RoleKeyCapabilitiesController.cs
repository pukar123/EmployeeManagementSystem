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
public sealed class RoleKeyCapabilitiesController : ControllerBase
{
    private readonly IRoleKeyCapabilityService _roleKeyCapabilities;

    public RoleKeyCapabilitiesController(IRoleKeyCapabilityService roleKeyCapabilities)
    {
        _roleKeyCapabilities = roleKeyCapabilities;
    }

    [HttpGet("{roleKey}")]
    [Authorize(Policy = "AdminAccess")]
    [ProducesResponseType(typeof(RoleCapabilityAccessResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleCapabilityAccessResponseModel>> GetByRoleKey(string roleKey, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roleKeyCapabilities.GetAsync(roleKey, cancellationToken);
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
        [FromBody] SetRoleCapabilityAccessRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _roleKeyCapabilities.SetAsync(roleKey, request, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
