using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmployeeHistoryController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeHistoryController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet("{employeeId:int}")]
    public async Task<ActionResult<EmployeeHistoryResponseModel>> GetByEmployeeId(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var history = await _employeeService.GetHistoryAsync(employeeId, cancellationToken);
        return history is null ? NotFound() : Ok(history);
    }
}
