using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Usermanagement.API.Filters;
using Pukar.Usermanagement.Application.Services.Internal;
using Pukar.Usermanagement.Contracts.ServiceAuth;
using Pukar.Usermanagement.Contracts.Users;

namespace Pukar.Usermanagement.API.Controllers.Internal;

[ApiController]
[Route("api/internal/v1/users")]
[ServiceAudit]
[InternalIdempotency]
public sealed class InternalUsersController : ControllerBase
{
    private readonly IInternalUserService _users;

    public InternalUsersController(IInternalUserService users)
    {
        _users = users;
    }

    [HttpPost("lookup")]
    [Authorize(Policy = ServiceScopes.UsersRead, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryResponseModel>>> BatchLookup(
        [FromBody] BatchUserLookupRequestModel request,
        CancellationToken cancellationToken)
    {
        var result = await _users.BatchLookupAsync(request.UserIds, cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-email")]
    [Authorize(Policy = ServiceScopes.UsersRead, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(typeof(UserSummaryResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummaryResponseModel>> GetByEmail(
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("{id:int}/activate")]
    [Authorize(Policy = ServiceScopes.UsersManage, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        return await _users.ActivateAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = ServiceScopes.UsersManage, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        return await _users.DeactivateAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/revoke-sessions")]
    [Authorize(Policy = ServiceScopes.UsersManage, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSessions(int id, CancellationToken cancellationToken)
    {
        await _users.RevokeAllSessionsAsync(id, cancellationToken);
        return NoContent();
    }
}
