using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Usermanagement.Application;
using Pukar.Usermanagement.Application.DTOs.Roles;
using Pukar.Usermanagement.Application.DTOs.UserRoles;
using Pukar.Usermanagement.Application.DTOs.Users;
using Pukar.Usermanagement.Application.Services.UserRoles;
using Pukar.Usermanagement.Application.Services.Users;
using Pukar.Shared;

namespace Pukar.Usermanagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = WellKnownRoles.Admin)]
public sealed class UsersController : ControllerBase
{
    private readonly IUserAdminService _users;
    private readonly IUserRoleService _userRoles;

    public UsersController(IUserAdminService users, IUserRoleService userRoles)
    {
        _users = users;
        _userRoles = userRoles;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryResponseModel>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _users.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserSummaryResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummaryResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _users.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserSummaryResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSummaryResponseModel>> Create(
        [FromBody] CreateUserRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _users.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateEmailException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserSummaryResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSummaryResponseModel>> Update(
        int id,
        [FromBody] UpdateUserRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _users.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (DuplicateEmailException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdminSetPassword(
        int id,
        [FromBody] AdminSetPasswordRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _users.AdminSetPasswordAsync(id, request, cancellationToken);
            return ok ? NoContent() : NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleResponseModel>>> GetRoles(int id, CancellationToken cancellationToken)
    {
        var roles = await _userRoles.GetRolesForUserAsync(id, cancellationToken);
        return Ok(roles);
    }

    [HttpPut("{id:int}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetRoles(
        int id,
        [FromBody] AssignUserRolesRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _userRoles.SetUserRolesAsync(id, request, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
