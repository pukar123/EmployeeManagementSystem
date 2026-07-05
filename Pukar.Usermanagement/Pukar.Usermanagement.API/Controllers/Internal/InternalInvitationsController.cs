using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;
using Pukar.Usermanagement.API.Filters;
using Pukar.Usermanagement.Application.Services.Invitations;
using Pukar.Usermanagement.Contracts.Invitations;
using Pukar.Usermanagement.Contracts.ServiceAuth;

namespace Pukar.Usermanagement.API.Controllers.Internal;

[ApiController]
[Route("api/internal/v1/invitations")]
[ServiceAudit]
[InternalIdempotency]
public sealed class InternalInvitationsController : ControllerBase
{
    private readonly IInvitationService _invitations;

    public InternalInvitationsController(IInvitationService invitations)
    {
        _invitations = invitations;
    }

    [HttpPost]
    [Authorize(Policy = ServiceScopes.InvitationsManage, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(typeof(InvitationResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvitationResponseModel>> Create(
        [FromBody] CreateInvitationRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _invitations.CreateOrSendAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (MapException(ex) is { } mapped)
        {
            return mapped;
        }
    }

    [HttpGet]
    [Authorize(Policy = ServiceScopes.InvitationsManage, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(typeof(IReadOnlyList<InvitationResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvitationResponseModel>>> List(
        [FromQuery] string correlationId,
        CancellationToken cancellationToken)
    {
        var result = await _invitations.ListByCorrelationIdAsync(correlationId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/resend")]
    [Authorize(Policy = ServiceScopes.InvitationsManage, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(typeof(InvitationResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvitationResponseModel>> Resend(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _invitations.ResendAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (MapException(ex) is { } mapped)
        {
            return mapped;
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = ServiceScopes.InvitationsManage, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Revoke(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _invitations.RevokeAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex) when (MapException(ex) is { } mapped)
        {
            return mapped;
        }
    }

    private static ObjectResult? MapException(Exception ex)
        => ex switch
        {
            ConflictBusinessRuleException conflict => new ConflictObjectResult(new { message = conflict.Message }),
            ConcurrencyConflictException concurrency => new ConflictObjectResult(new { message = concurrency.Message }),
            BusinessRuleException rule => new BadRequestObjectResult(new { message = rule.Message }),
            _ => null,
        };
}
