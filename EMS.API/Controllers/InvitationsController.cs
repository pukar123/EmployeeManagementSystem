using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/invitations")]
public sealed class InvitationsController : ControllerBase
{
    private readonly IEmployeeInvitationService _invitations;

    public InvitationsController(IEmployeeInvitationService invitations)
    {
        _invitations = invitations;
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept(
        [FromBody] AcceptEmployeeInvitationRequestModel request,
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
