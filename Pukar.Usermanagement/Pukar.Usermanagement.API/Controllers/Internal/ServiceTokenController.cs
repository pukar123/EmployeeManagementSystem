using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Services.ServiceAuth;
using Pukar.Usermanagement.Contracts.ServiceAuth;

namespace Pukar.Usermanagement.API.Controllers.Internal;

[ApiController]
[Route("api/internal/v1/service-token")]
public sealed class ServiceTokenController : ControllerBase
{
    private readonly IServiceAuthService _serviceAuth;

    public ServiceTokenController(IServiceAuthService serviceAuth)
    {
        _serviceAuth = serviceAuth;
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceTokenResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServiceTokenResponseModel>> IssueToken(
        [FromBody] ServiceTokenRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _serviceAuth.IssueTokenAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedServiceException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
