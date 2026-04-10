using EMS.API.Services;
using EMS.Application.DTOs.Organization;
using EMS.Application.Mapping;
using Pukar.Shared;
using EMS.Application.Services.Organizations;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly LocalOrganizationLogoStorage _logoStorage;

    public OrganizationsController(
        IOrganizationService organizationService,
        LocalOrganizationLogoStorage logoStorage)
    {
        _organizationService = organizationService;
        _logoStorage = logoStorage;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizationResponseModel>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _organizationService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrganizationResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _organizationService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationResponseModel>> Create(
        [FromBody] OrganizationDTO dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _organizationService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponseModel());
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrganizationResponseModel>> Update(
        int id,
        [FromBody] UpdateOrganizationRequestModel request,
        CancellationToken cancellationToken)
    {
        var updated = await _organizationService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Upload a logo image; file is stored on disk under wwwroot/attachments/Organization. Only the relative URL path is saved in the database.</summary>
    [HttpPost("{id:int}/logo")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<OrganizationResponseModel>> UploadLogo(
        int id,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "A logo file is required." });

        var existing = await _organizationService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        try
        {
            _logoStorage.TryDeleteFile(existing.LogoRelativePath);
            var relativePath = await _logoStorage.SaveLogoAsync(id, file, cancellationToken);
            var updated = await _organizationService.UpdateLogoRelativePathAsync(id, relativePath, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _organizationService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
