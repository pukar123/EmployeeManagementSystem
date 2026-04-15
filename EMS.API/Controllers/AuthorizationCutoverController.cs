using EMS.Application.Services.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Usermanagement.Application;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/authorization-cutover")]
[Authorize(Roles = WellKnownRoles.Admin)]
public sealed class AuthorizationCutoverController : ControllerBase
{
    private readonly IAuthorizationCutoverReadinessReporter _readinessReporter;

    public AuthorizationCutoverController(IAuthorizationCutoverReadinessReporter readinessReporter)
    {
        _readinessReporter = readinessReporter;
    }

    [HttpGet("readiness")]
    [ProducesResponseType(typeof(AuthorizationCutoverReadinessSnapshot), StatusCodes.Status200OK)]
    public ActionResult<AuthorizationCutoverReadinessSnapshot> GetReadiness()
    {
        return Ok(_readinessReporter.GetReadinessSnapshot());
    }
}
