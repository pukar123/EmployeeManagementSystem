using EMS.Application.DTOs.Shift;
using EMS.Application.Services.Shifts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;
using Pukar.Usermanagement.Contracts.Roles;

namespace EMS.API.Controllers;

[ApiController]
[Authorize(Roles = WellKnownRoles.Admin)]
[Route("api/[controller]")]
public sealed class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;

    public ShiftsController(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShiftResponseModel>>> GetAll(
        [FromQuery] int? organizationId,
        [FromQuery] int? employeeId,
        CancellationToken cancellationToken)
    {
        var items = await _shiftService.GetAllAsync(organizationId, employeeId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShiftResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _shiftService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ShiftResponseModel>> Create(
        [FromBody] CreateShiftRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _shiftService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ShiftResponseModel>> Update(
        int id,
        [FromBody] UpdateShiftRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _shiftService.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _shiftService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
