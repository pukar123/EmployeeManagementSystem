using EMS.Application.DTOs.Onboarding;
using EMS.Application.Services.Employees;
using EMS.Application.Services.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/employees/{employeeId:int}/onboarding")]
public sealed class EmployeeOnboardingController : ControllerBase
{
    private readonly IOnboardingChecklistService _onboardingChecklistService;
    private readonly IEmployeeAccessService _employeeAccessService;

    public EmployeeOnboardingController(
        IOnboardingChecklistService onboardingChecklistService,
        IEmployeeAccessService employeeAccessService)
    {
        _onboardingChecklistService = onboardingChecklistService;
        _employeeAccessService = employeeAccessService;
    }

    [HttpGet]
    public async Task<ActionResult<EmployeeOnboardingProgressResponseModel>> GetProgress(
        int employeeId,
        CancellationToken cancellationToken)
    {
        await _employeeAccessService.EnsureCanViewEmployeeProfileAsync(employeeId, cancellationToken);
        var progress = await _onboardingChecklistService.GetProgressAsync(employeeId, cancellationToken);
        return Ok(progress);
    }
}
