using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Pukar.Usermanagement.API.Filters;
using Pukar.Usermanagement.API.RateLimiting;
using Pukar.Usermanagement.Contracts.Auth;
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

    /// <summary>
    /// Self-registration. Off by default (<c>Registration:AllowSelfRegistration</c>): returns 404 with
    /// code <c>registration_disabled</c> for an empty body or any well-formed JSON body. Malformed or
    /// wrong-type JSON is rejected by the framework with 400 before this action runs; nothing is created.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicies.Register)]
    [AuthRequestSizeLimit]
    [ProducesResponseType(typeof(AuthResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponseModel>> Register(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RegisterRequestModel? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _auth.RegisterAsync(request ?? new RegisterRequestModel(), ClientInfo(), cancellationToken);
            return Ok(result);
        }
        catch (FeatureDisabledException ex)
        {
            return NotFound(new { code = ex.Code, message = ex.Message });
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
    [EnableRateLimiting(AuthRateLimitPolicies.SignIn)]
    [AuthRequestSizeLimit]
    [EmailRateLimit(AuthRateLimitPolicies.SignInEmail)]
    [ProducesResponseType(typeof(AuthResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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
    [EnableRateLimiting(AuthRateLimitPolicies.Refresh)]
    [AuthRequestSizeLimit]
    [ProducesResponseType(typeof(AuthResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicies.ForgotPassword)]
    [AuthRequestSizeLimit]
    [EmailRateLimit(AuthRateLimitPolicies.ForgotPasswordEmail)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestModel request,
        CancellationToken cancellationToken)
    {
        await _auth.RequestPasswordResetAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicies.ResetPassword)]
    [AuthRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _auth.ResetPasswordAsync(request, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestModel request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return BadRequest(new { message = "Current user could not be resolved." });

        try
        {
            await _auth.ChangePasswordAsync(userId.Value, request, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string? ClientInfo()
    {
        if (Request.Headers.TryGetValue("User-Agent", out var ua) && !string.IsNullOrEmpty(ua))
            return ua.ToString();
        return null;
    }

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var id) ? id : null;
    }
}
