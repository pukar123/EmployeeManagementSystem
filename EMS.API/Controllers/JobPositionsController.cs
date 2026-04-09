using EMS.Application.DTOs.JobPosition;
using Pukar.Shared;
using EMS.Application.Services.JobPositions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobPositionsController : ControllerBase
{
    private readonly IJobPositionService _jobPositionService;

    public JobPositionsController(IJobPositionService jobPositionService)
    {
        _jobPositionService = jobPositionService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobPositionResponseModel>>> GetByOrganization(
        [FromQuery] int organizationId,
        CancellationToken cancellationToken)
    {
        var items = await _jobPositionService.GetByOrganizationAsync(organizationId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobPositionResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _jobPositionService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<JobPositionResponseModel>> Create(
        [FromBody] CreateJobPositionRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _jobPositionService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<JobPositionResponseModel>> Update(
        int id,
        [FromBody] UpdateJobPositionRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _jobPositionService.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _jobPositionService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
