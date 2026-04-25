using EMS.Application.Services.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminAccess")]
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
