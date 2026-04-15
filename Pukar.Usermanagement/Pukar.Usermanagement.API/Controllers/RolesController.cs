using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Usermanagement.Application;
using Pukar.Usermanagement.Application.DTOs.Roles;
using Pukar.Usermanagement.Application.Services.Roles;
using Pukar.Shared;

namespace Pukar.Usermanagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _roles;

    public RolesController(IRoleService roles)
    {
        _roles = roles;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleResponseModel>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _roles.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("metadata/v1")]
    [Authorize(Roles = WellKnownRoles.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleMetadataV1ResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleMetadataV1ResponseModel>>> GetMetadataV1(CancellationToken cancellationToken)
    {
        var items = await _roles.GetMetadataV1Async(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RoleResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _roles.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = WellKnownRoles.Admin)]
    [ProducesResponseType(typeof(RoleResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleResponseModel>> Create(
        [FromBody] CreateRoleRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _roles.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = WellKnownRoles.Admin)]
    [ProducesResponseType(typeof(RoleResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleResponseModel>> Update(
        int id,
        [FromBody] UpdateRoleRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _roles.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = WellKnownRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _roles.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
