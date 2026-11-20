using EMS.API.Auth;
using EMS.Application.DTOs.Location;
using EMS.Application.Services.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationsController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocationResponseModel>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _locationService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LocationResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _locationService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [Authorize(Policy = AuthorizationPolicyNames.OrganizationAdmin)]
    [HttpPost]
    public async Task<ActionResult<LocationResponseModel>> Create(
        [FromBody] CreateLocationRequestModel request,
        CancellationToken cancellationToken)
    {
        var created = await _locationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Policy = AuthorizationPolicyNames.OrganizationAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<LocationResponseModel>> Update(
        int id,
        [FromBody] UpdateLocationRequestModel request,
        CancellationToken cancellationToken)
    {
        var updated = await _locationService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [Authorize(Policy = AuthorizationPolicyNames.OrganizationAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _locationService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
