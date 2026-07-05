using EMS.Application.DTOs.JobPosition;
using EMS.Application.Services.Integrations;
using EMS.Application.Services.JobPositions;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/JobPositions/{jobPositionId:int}/roles")]
public sealed class JobPositionRolesController : ControllerBase
{
    private readonly IPositionRoleService _positionRoleService;

    public JobPositionRolesController(IPositionRoleService positionRoleService)
    {
        _positionRoleService = positionRoleService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PositionRoleResponseModel>>> GetByPosition(
        int jobPositionId,
        CancellationToken cancellationToken)
    {
        var items = await _positionRoleService.GetByPositionAsync(jobPositionId, cancellationToken);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpPut]
    public async Task<IActionResult> SetPositionRoles(
        int jobPositionId,
        [FromBody] SetPositionRolesRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _positionRoleService.SetPositionRolesAsync(jobPositionId, request, cancellationToken);
            return updated ? NoContent() : NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
        catch (UserManagementDependencyUnavailableException ex)
        {
            return EmployeeControllerHelpers.HandleDependencyUnavailable(ex);
        }
    }
}
