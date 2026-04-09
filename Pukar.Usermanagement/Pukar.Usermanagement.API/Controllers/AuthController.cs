using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Usermanagement.Application.DTOs.Auth;
using Pukar.Usermanagement.Application.Services.Auth;
using Pukar.Shared;

namespace Pukar.Usermanagement.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseModel>> Register(
        [FromBody] RegisterRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _auth.RegisterAsync(request, ClientInfo(), cancellationToken);
            return Ok(result);
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

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseModel>> Login(
        [FromBody] LoginRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _auth.LoginAsync(request, ClientInfo(), cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseModel>> Refresh(
        [FromBody] RefreshTokenRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _auth.RefreshAsync(request, ClientInfo(), cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("revoke")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(
        [FromBody] RefreshTokenRequestModel request,
        CancellationToken cancellationToken)
    {
        await _auth.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    private string? ClientInfo()
    {
        if (Request.Headers.TryGetValue("User-Agent", out var ua) && !string.IsNullOrEmpty(ua))
            return ua.ToString();
        return null;
    }
}
