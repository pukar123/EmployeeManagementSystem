using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Leave;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveTypesController : ControllerBase
{
    private readonly ILeaveTypeService _leaveTypeService;

    public LeaveTypesController(ILeaveTypeService leaveTypeService)
    {
        _leaveTypeService = leaveTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaveTypeResponseModel>>> GetByOrganization(
        [FromQuery] int organizationId,
        CancellationToken cancellationToken)
    {
        var result = await _leaveTypeService.GetByOrganizationAsync(organizationId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveTypeResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _leaveTypeService.GetByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<LeaveTypeResponseModel>> Create(
        [FromBody] CreateLeaveTypeRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _leaveTypeService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LeaveTypeResponseModel>> Update(
        int id,
        [FromBody] UpdateLeaveTypeRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _leaveTypeService.UpdateAsync(id, request, cancellationToken);
            return Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
