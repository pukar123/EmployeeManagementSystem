using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Leave;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveOperationsController : ControllerBase
{
    private readonly ILeaveAccrualService _leaveAccrualService;

    public LeaveOperationsController(ILeaveAccrualService leaveAccrualService)
    {
        _leaveAccrualService = leaveAccrualService;
    }

    [HttpPost("accrual/run")]
    public async Task<ActionResult<LeaveAccrualRunResultResponseModel>> RunAccrual(
        [FromBody] RunLeaveAccrualRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _leaveAccrualService.RunAccrualAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("year-reset/run")]
    public async Task<ActionResult<LeaveYearResetResultResponseModel>> RunLeaveYearReset(
        [FromBody] RunLeaveYearResetRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _leaveAccrualService.RunLeaveYearResetAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
