using EMS.Application.Services.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/authorization-cutover")]
[Authorize(Policy = "AdminAccess")]
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
