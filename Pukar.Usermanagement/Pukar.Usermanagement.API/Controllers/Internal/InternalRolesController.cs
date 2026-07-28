using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;
using Pukar.Usermanagement.API.Filters;
using Pukar.Usermanagement.Application.Services.Internal;
using Pukar.Usermanagement.Contracts.Roles;
using Pukar.Usermanagement.Contracts.ServiceAuth;
using Pukar.Usermanagement.Contracts.Users;

namespace Pukar.Usermanagement.API.Controllers.Internal;

[ApiController]
[Route("api/internal/v1")]
[ServiceAudit]
[InternalIdempotency]
public sealed class InternalRolesController : ControllerBase
{
    private readonly IInternalRoleService _roles;

    public InternalRolesController(IInternalRoleService roles)
    {
        _roles = roles;
    }

    [HttpGet("roles/metadata")]
    [Authorize(Policy = ServiceScopes.RolesRead, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleMetadataV1ResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleMetadataV1ResponseModel>>> GetMetadata(CancellationToken cancellationToken)
    {
        var result = await _roles.GetMetadataAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("users/{id:int}/roles")]
    [Authorize(Policy = ServiceScopes.RolesManage, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(typeof(UserRoleKeysResponseModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserRoleKeysResponseModel>> GetUserRoles(int id, CancellationToken cancellationToken)
    {
        var keys = await _roles.GetUserRoleKeysAsync(id, cancellationToken);
        return Ok(new UserRoleKeysResponseModel { NormalizedRoleKeys = keys });
    }

    [HttpPut("users/{id:int}/roles")]
    [Authorize(Policy = ServiceScopes.RolesManage, AuthenticationSchemes = ServiceAuthConstants.ServiceTokenScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReplaceRoles(
        int id,
        [FromBody] ReplaceUserRolesByKeysRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _roles.ReplaceUserRolesByKeysAsync(id, request.NormalizedRoleKeys, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
