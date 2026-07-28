using EMS.Application.DTOs.Onboarding;
using EMS.Application.Services.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;
using Pukar.Usermanagement.Contracts.Roles;

namespace EMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/onboarding/templates")]
public sealed class OnboardingChecklistTemplatesController : ControllerBase
{
    private readonly IOnboardingChecklistTemplateService _templateService;

    public OnboardingChecklistTemplatesController(IOnboardingChecklistTemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OnboardingChecklistTemplateResponseModel>>> GetByOrganization(
        [FromQuery] int organizationId,
        CancellationToken cancellationToken)
    {
        var result = await _templateService.GetByOrganizationAsync(organizationId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OnboardingChecklistTemplateResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _templateService.GetByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost]
    [Authorize(Roles = WellKnownRoles.Admin)]
    public async Task<ActionResult<OnboardingChecklistTemplateResponseModel>> Create(
        [FromBody] CreateOnboardingChecklistTemplateRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _templateService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = WellKnownRoles.Admin)]
    public async Task<ActionResult<OnboardingChecklistTemplateResponseModel>> Update(
        int id,
        [FromBody] UpdateOnboardingChecklistTemplateRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _templateService.UpdateAsync(id, request, cancellationToken);
            return Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = WellKnownRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _templateService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    private ActionResult HandleBusinessRule(BusinessRuleException ex)
    {
        if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { message = ex.Message });
        return BadRequest(new { message = ex.Message });
    }
}
