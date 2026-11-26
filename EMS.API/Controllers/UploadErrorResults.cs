using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EMS.Application.DTOs.Files;
using EMS.Application.Services.Files;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

/// <summary>
/// Turns an <see cref="UploadRejectedException"/> into the B10 error response: 413 for a file over the limit, 400
/// otherwise, with the body <c>{ code, message }</c>. The reason is logged at Information with the user id; the file
/// contents never are, and the message does not say which check failed.
/// </summary>
internal static class UploadErrorResults
{
    public static ObjectResult Rejected(ILogger logger, ClaimsPrincipal user, UploadRejectedException ex)
    {
        var userId = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        logger.LogInformation(
            "Upload refused for {Purpose}: {Reason} ({FailedCheck}) by user {UserId}",
            ex.Purpose,
            ex.Reason,
            ex.FailedCheck,
            userId);

        var status = ex.Reason == UploadRejectionReason.TooLarge
            ? StatusCodes.Status413PayloadTooLarge
            : StatusCodes.Status400BadRequest;
        return new ObjectResult(new { code = ex.Code, message = ex.Message }) { StatusCode = status };
    }
}
