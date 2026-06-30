using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/Employees/{employeeId:int}/roles")]
[Authorize]
public sealed class EmployeeRolesController : ControllerBase
{
    private readonly IEmployeeRoleService _employeeRoleService;
    private readonly IEmployeeAccessService _employeeAccessService;

    public EmployeeRolesController(
        IEmployeeRoleService employeeRoleService,
        IEmployeeAccessService employeeAccessService)
    {
        _employeeRoleService = employeeRoleService;
        _employeeAccessService = employeeAccessService;
    }

    [HttpGet("effective")]
    public async Task<ActionResult<IReadOnlyList<EmployeeEffectiveRoleResponseModel>>> GetEffectiveRoles(
        int employeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanViewEmployeesAsync(cancellationToken);
            var items = await _employeeRoleService.GetEffectiveRolesAsync(employeeId, cancellationToken);
            return items is null ? NotFound() : Ok(items);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }

    [HttpPut("direct-overrides")]
    public async Task<IActionResult> SetDirectOverrides(
        int employeeId,
        [FromBody] SetEmployeeDirectRolesRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanManageEmployeesAsync(cancellationToken);
            var updated = await _employeeRoleService.SetDirectRolesAsync(employeeId, request, cancellationToken);
            return updated ? NoContent() : NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }
}
