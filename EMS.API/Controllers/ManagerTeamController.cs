using EMS.Application.DTOs.Manager;
using EMS.Application.Services.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/Manager")]
[Authorize]
public sealed class ManagerTeamController : ControllerBase
{
    private readonly IManagerTeamService _managerTeamService;

    public ManagerTeamController(IManagerTeamService managerTeamService)
    {
        _managerTeamService = managerTeamService;
    }

    [HttpGet("team")]
    public async Task<ActionResult<ManagerTeamDashboardResponseModel>> GetTeam(
        [FromQuery] ManagerTeamQueryModel query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _managerTeamService.GetDashboardAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }
}
