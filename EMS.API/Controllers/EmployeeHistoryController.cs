using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmployeeHistoryController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IEmployeeAccessService _employeeAccessService;

    public EmployeeHistoryController(
        IEmployeeService employeeService,
        IEmployeeAccessService employeeAccessService)
    {
        _employeeService = employeeService;
        _employeeAccessService = employeeAccessService;
    }

    [HttpGet("{employeeId:int}")]
    public async Task<ActionResult<EmployeeHistoryResponseModel>> GetByEmployeeId(
        int employeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _employeeAccessService.EnsureCanViewEmployeesAsync(cancellationToken);
            var history = await _employeeService.GetHistoryAsync(employeeId, cancellationToken);
            return history is null ? NotFound() : Ok(history);
        }
        catch (BusinessRuleException ex)
        {
            return EmployeeControllerHelpers.HandleBusinessRule(ex);
        }
    }
}
