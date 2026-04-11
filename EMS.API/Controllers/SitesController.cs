using EMS.Application.DTOs.Site;
using EMS.Application.DTOs.Employee;
using EMS.Application.Services.EmployeeSites;
using EMS.Application.Services.Sites;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SitesController : ControllerBase
{
    private readonly ISiteService _siteService;
    private readonly IEmployeeSiteService _employeeSiteService;

    public SitesController(ISiteService siteService, IEmployeeSiteService employeeSiteService)
    {
        _siteService = siteService;
        _employeeSiteService = employeeSiteService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteResponseModel>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _siteService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SiteResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _siteService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<SiteResponseModel>> Create(
        [FromBody] CreateSiteRequestModel request,
        CancellationToken cancellationToken)
    {
        var created = await _siteService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.SiteId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SiteResponseModel>> Update(
        int id,
        [FromBody] UpdateSiteRequestModel request,
        CancellationToken cancellationToken)
    {
        var updated = await _siteService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _siteService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{siteId:int}/employees")]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponseModel>>> GetEmployeesForSite(
        int siteId,
        CancellationToken cancellationToken)
    {
        var items = await _employeeSiteService.GetEmployeesForSiteAsync(siteId, cancellationToken);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpPost("{siteId:int}/employees")]
    public async Task<IActionResult> LinkEmployee(
        int siteId,
        [FromBody] LinkEmployeeToSiteRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeSiteService.LinkAsync(siteId, request.EmployeeId, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{siteId:int}/employees/{employeeId:int}")]
    public async Task<IActionResult> UnlinkEmployee(int siteId, int employeeId, CancellationToken cancellationToken)
    {
        var ok = await _employeeSiteService.UnlinkAsync(siteId, employeeId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
