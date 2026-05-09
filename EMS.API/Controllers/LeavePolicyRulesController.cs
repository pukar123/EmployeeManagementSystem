using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LeavePolicyRulesController : ControllerBase
{
    private readonly ILeavePolicyRuleService _policyRuleService;

    public LeavePolicyRulesController(ILeavePolicyRuleService policyRuleService)
    {
        _policyRuleService = policyRuleService;
    }

    [HttpGet("leave-type/{leaveTypeId:int}")]
    public async Task<ActionResult<IReadOnlyList<LeavePolicyRuleResponseModel>>> GetByLeaveType(
        int leaveTypeId,
        CancellationToken cancellationToken)
    {
        var result = await _policyRuleService.GetByLeaveTypeAsync(leaveTypeId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeavePolicyRuleResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _policyRuleService.GetByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<LeavePolicyRuleResponseModel>> Create(
        [FromBody] CreateLeavePolicyRuleRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _policyRuleService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LeavePolicyRuleResponseModel>> Update(
        int id,
        [FromBody] UpdateLeavePolicyRuleRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _policyRuleService.UpdateAsync(id, request, cancellationToken);
            return Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _policyRuleService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
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
