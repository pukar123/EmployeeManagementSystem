using EMS.API.Auth;
using EMS.API.Filters;
using EMS.API.Services;
using EMS.Application.DTOs.Files;
using EMS.Application.DTOs.Organization;
using EMS.Application.Services.Files;
using EMS.Application.Mapping;
using Pukar.Shared;
using EMS.Application.Services.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly LocalOrganizationLogoStorage _logoStorage;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(
        IOrganizationService organizationService,
        LocalOrganizationLogoStorage logoStorage,
        ILogger<OrganizationsController> logger)
    {
        _organizationService = organizationService;
        _logoStorage = logoStorage;
        _logger = logger;
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

    [Authorize(Policy = AuthorizationPolicyNames.OrganizationAdmin)]
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

    [Authorize(Policy = AuthorizationPolicyNames.OrganizationAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrganizationResponseModel>> Update(
        int id,
        [FromBody] UpdateOrganizationRequestModel request,
        CancellationToken cancellationToken)
    {
        var updated = await _organizationService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>
    /// Upload a logo image (B10: PNG, JPEG or WebP, up to 5 MB; never SVG); the file is stored on disk under
    /// wwwroot/attachments/Organization. Logos are public: only the public logo path (served by static files at
    /// /attachments/Organization) is saved in the database. The new logo is checked and saved, and the database
    /// updated, before the old file is deleted, so a refused upload leaves the current logo unchanged.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicyNames.OrganizationAdmin)]
    [HttpPost("{id:int}/logo")]
    [UploadRequestSizeLimit(UploadPurpose.OrganizationLogo)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<OrganizationResponseModel>> UploadLogo(
        int id,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        // A missing file part is "A logo file is required."; a zero-byte file is refused by the checker as file_empty.
        if (file is null)
            return BadRequest(new { message = "A logo file is required." });

        var existing = await _organizationService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        string? newPath = null;
        var saved = false;
        try
        {
            newPath = await _logoStorage.SaveLogoAsync(id, file, cancellationToken);
            var updated = await _organizationService.UpdateLogoRelativePathAsync(id, newPath, cancellationToken);
            if (updated is null)
                return NotFound();

            saved = true;
            if (!string.Equals(existing.LogoRelativePath, newPath, StringComparison.Ordinal))
                _logoStorage.TryDeleteFile(existing.LogoRelativePath);
            return Ok(updated);
        }
        catch (UploadRejectedException ex)
        {
            return UploadErrorResults.Rejected(_logger, User, ex);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        finally
        {
            // Any failure after the new file was written removes it; the old file and database path are untouched.
            if (!saved && newPath is not null)
                _logoStorage.TryDeleteFile(newPath);
        }
    }

    [Authorize(Policy = AuthorizationPolicyNames.OrganizationAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _organizationService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
