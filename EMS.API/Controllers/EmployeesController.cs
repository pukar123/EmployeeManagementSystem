using EMS.Application.DTOs.Employee;
using EMS.Application.DTOs.Site;
using Pukar.Shared;
using EMS.Application.Services.EmployeeSites;
using EMS.Application.Services.Employees;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IEmployeeIdentityProvisioningService _employeeIdentityProvisioningService;
    private readonly IEmployeeSiteService _employeeSiteService;

    public EmployeesController(
        IEmployeeService employeeService,
        IEmployeeIdentityProvisioningService employeeIdentityProvisioningService,
        IEmployeeSiteService employeeSiteService)
    {
        _employeeService = employeeService;
        _employeeIdentityProvisioningService = employeeIdentityProvisioningService;
        _employeeSiteService = employeeSiteService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponseModel>>> GetAll(
        CancellationToken cancellationToken)
    {
        var items = await _employeeService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}/sites")]
    public async Task<ActionResult<IReadOnlyList<SiteResponseModel>>> GetSitesForEmployee(
        int id,
        CancellationToken cancellationToken)
    {
        var items = await _employeeSiteService.GetSitesForEmployeeAsync(id, cancellationToken);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeResponseModel>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var item = await _employeeService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponseModel>> Create(
        [FromBody] CreateEmployeeRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _employeeService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id:int}/provision-user")]
    public async Task<ActionResult<ProvisionEmployeeUserResponseModel>> ProvisionUser(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _employeeIdentityProvisioningService.ProvisionAsync(id, cancellationToken);
            return Ok(response);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:int}/linked-user/roles")]
    public async Task<IActionResult> AssignLinkedUserRoles(
        int id,
        [FromBody] AssignEmployeeUserRolesRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeIdentityProvisioningService.AssignRolesAsync(id, request, cancellationToken);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeResponseModel>> Update(
        int id,
        [FromBody] UpdateEmployeeRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _employeeService.UpdateAsync(id, request, cancellationToken);
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
        var deleted = await _employeeService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
