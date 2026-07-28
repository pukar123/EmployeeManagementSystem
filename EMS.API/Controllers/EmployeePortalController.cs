using EMS.Application.DTOs.EmployeePortal;
using EMS.Application.DTOs.Shift;
using EMS.Application.DTOs.Task;
using EMS.Application.Services.EmployeePortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class EmployeePortalController : ControllerBase
{
    private readonly IEmployeePortalService _employeePortalService;

    public EmployeePortalController(IEmployeePortalService employeePortalService)
    {
        _employeePortalService = employeePortalService;
    }

    [HttpGet("eligibility")]
    public async Task<ActionResult<EmployeePortalEligibilityResponseModel>> GetEligibility(CancellationToken cancellationToken)
    {
        var result = await _employeePortalService.GetEligibilityAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<EmployeePortalResponseModel>> GetPortal(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _employeePortalService.GetPortalAsync(cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost("shifts/{id:int}/start")]
    public async Task<ActionResult<ShiftResponseModel>> StartShift(int id, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _employeePortalService.StartShiftAsync(id, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost("tasks/{id:int}/start")]
    public async Task<ActionResult<TaskResponseModel>> StartTask(int id, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _employeePortalService.StartTaskAsync(id, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    private ActionResult HandleBusinessRule(BusinessRuleException ex)
    {
        if (ex.Message.Contains("not authenticated", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { message = ex.Message });

        if (ex.Message.Contains("linked", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { message = ex.Message });

        return BadRequest(new { message = ex.Message });
    }
}
