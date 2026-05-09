using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using Pukar.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/Employees/{employeeId:int}/roles")]
public sealed class EmployeeRolesController : ControllerBase
{
    private readonly IEmployeeRoleService _employeeRoleService;

    public EmployeeRolesController(IEmployeeRoleService employeeRoleService)
    {
        _employeeRoleService = employeeRoleService;
    }

    [HttpGet("effective")]
    public async Task<ActionResult<IReadOnlyList<EmployeeEffectiveRoleResponseModel>>> GetEffectiveRoles(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var items = await _employeeRoleService.GetEffectiveRolesAsync(employeeId, cancellationToken);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpPut("direct-overrides")]
    public async Task<IActionResult> SetDirectOverrides(
        int employeeId,
        [FromBody] SetEmployeeDirectRolesRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _employeeRoleService.SetDirectRolesAsync(employeeId, request, cancellationToken);
            return updated ? NoContent() : NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
