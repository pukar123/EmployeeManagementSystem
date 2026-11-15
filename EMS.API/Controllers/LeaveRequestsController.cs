using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly ILeaveApprovalService _leaveApprovalService;

    public LeaveRequestsController(
        ILeaveRequestService leaveRequestService,
        ILeaveApprovalService leaveApprovalService)
    {
        _leaveRequestService = leaveRequestService;
        _leaveApprovalService = leaveApprovalService;
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestResponseModel>>> GetByEmployee(
        int employeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _leaveRequestService.GetByEmployeeAsync(employeeId, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpGet("admin/summary")]
    public async Task<ActionResult<LeaveAdminSummaryResponseModel>> GetAdminSummary(
        [FromQuery] int organizationId,
        [FromQuery] DateTime? asOfDateUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _leaveRequestService.GetAdminSummaryAsync(
                organizationId,
                asOfDateUtc ?? DateTime.UtcNow,
                cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveRequestResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _leaveRequestService.GetByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestResponseModel>> Create(
        [FromBody] CreateLeaveRequestRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _leaveRequestService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LeaveRequestResponseModel>> Update(
        int id,
        [FromBody] UpdateLeaveRequestRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _leaveRequestService.UpdateAsync(id, request, cancellationToken);
            return Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<LeaveRequestResponseModel>> Cancel(int id, CancellationToken cancellationToken)
    {
        try
        {
            var cancelled = await _leaveRequestService.CancelAsync(id, cancellationToken);
            return Ok(cancelled);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<LeaveRequestResponseModel>> Approve(
        int id,
        [FromBody] ApproveLeaveRequestRequestModel? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var approved = await _leaveApprovalService.ApproveAsync(
                id,
                request ?? new ApproveLeaveRequestRequestModel(),
                cancellationToken);
            return Ok(approved);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<LeaveRequestResponseModel>> Reject(
        int id,
        [FromBody] RejectLeaveRequestRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rejected = await _leaveApprovalService.RejectAsync(id, request, cancellationToken);
            return Ok(rejected);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    private ActionResult HandleBusinessRule(BusinessRuleException ex)
    {
        if (string.Equals(ex.Message, LeaveAccessMessages.Denied, StringComparison.Ordinal))
            return Forbid();

        if (LeaveApprovalMessages.IsAccessDenied(ex.Message))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });

        if (string.Equals(ex.Message, LeaveApprovalMessages.ConcurrencyConflict, StringComparison.Ordinal))
            return Conflict(new { message = ex.Message });

        if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { message = ex.Message });
        return BadRequest(new { message = ex.Message });
    }
}
