using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/employee-access")]
[Authorize]
public sealed class EmployeeAccessController : ControllerBase
{
    private readonly IEmployeeAccessService _employeeAccessService;

    public EmployeeAccessController(IEmployeeAccessService employeeAccessService)
    {
        _employeeAccessService = employeeAccessService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(EmployeeAccessCapabilitiesResponseModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeAccessCapabilitiesResponseModel>> GetMyCapabilities(CancellationToken cancellationToken)
    {
        var capabilities = await _employeeAccessService.GetMyCapabilitiesAsync(cancellationToken);
        return Ok(capabilities);
    }
}
