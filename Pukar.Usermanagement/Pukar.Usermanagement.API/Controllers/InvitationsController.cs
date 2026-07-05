using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Services.Invitations;
using Pukar.Usermanagement.Contracts.Invitations;

namespace Pukar.Usermanagement.API.Controllers;

[ApiController]
[Route("api/invitations")]
public sealed class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitations;

    public InvitationsController(IInvitationService invitations)
    {
        _invitations = invitations;
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Accept(
        [FromBody] AcceptInvitationRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _invitations.AcceptAsync(request, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
