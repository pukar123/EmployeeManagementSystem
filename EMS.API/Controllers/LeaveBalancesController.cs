using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LeaveBalancesController : ControllerBase
{
    private readonly ILeaveBalanceService _leaveBalanceService;

    public LeaveBalancesController(ILeaveBalanceService leaveBalanceService)
    {
        _leaveBalanceService = leaveBalanceService;
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IReadOnlyList<LeaveBalanceResponseModel>>> GetByEmployee(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var result = await _leaveBalanceService.GetByEmployeeAsync(employeeId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("employee/{employeeId:int}/type/{leaveTypeId:int}")]
    public async Task<ActionResult<LeaveBalanceResponseModel>> GetByEmployeeAndType(
        int employeeId,
        int leaveTypeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _leaveBalanceService.GetByEmployeeAndTypeAsync(
                employeeId,
                leaveTypeId,
                cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    private ActionResult HandleBusinessRule(BusinessRuleException ex)
    {
        if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { message = ex.Message });
        return BadRequest(new { message = ex.Message });
    }
}
