using EMS.Application.Services.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Usermanagement.Application;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = WellKnownRoles.Admin)]
public sealed class AuthorizationTelemetryController : ControllerBase
{
    private readonly IAuthorizationTelemetryReporter _reporter;

    public AuthorizationTelemetryController(IAuthorizationTelemetryReporter reporter)
    {
        _reporter = reporter;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AuthorizationTelemetrySnapshot), StatusCodes.Status200OK)]
    public ActionResult<AuthorizationTelemetrySnapshot> Get()
    {
        return Ok(_reporter.GetSnapshot());
    }
}
