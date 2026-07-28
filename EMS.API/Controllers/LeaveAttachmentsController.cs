using EMS.API.Services;
using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LeaveAttachmentsController : ControllerBase
{
    private readonly ILeaveAttachmentService _attachmentService;
    private readonly LocalLeaveAttachmentStorage _storage;
    private readonly IWebHostEnvironment _env;

    public LeaveAttachmentsController(
        ILeaveAttachmentService attachmentService,
        LocalLeaveAttachmentStorage storage,
        IWebHostEnvironment env)
    {
        _attachmentService = attachmentService;
        _storage = storage;
        _env = env;
    }

    [HttpGet("request/{leaveRequestId:int}")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestAttachmentResponseModel>>> GetByRequest(
        int leaveRequestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attachmentService.GetByRequestIdAsync(leaveRequestId, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpPost]
    [RequestSizeLimit(16 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 16 * 1024 * 1024)]
    public async Task<ActionResult<LeaveRequestAttachmentResponseModel>> Upload(
        [FromForm] CreateLeaveAttachmentForm request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
            return BadRequest(new { message = "A file is required." });

        string? relativePath = null;
        try
        {
            var file = await _storage.SaveAsync(request.LeaveRequestId, request.File, cancellationToken);
            relativePath = file.RelativePath;
            var created = await _attachmentService.AddAsync(
                request.LeaveRequestId,
                file.FileName,
                file.ContentType,
                file.Size,
                file.RelativePath,
                cancellationToken);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            if (!string.IsNullOrWhiteSpace(relativePath))
                _storage.TryDelete(relativePath);
            return BadRequest(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            if (!string.IsNullOrWhiteSpace(relativePath))
                _storage.TryDelete(relativePath);
            return HandleBusinessRule(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var attachment = await _attachmentService.GetByIdAsync(id, cancellationToken);
            _storage.TryDelete(attachment.StoragePath);
            var deleted = await _attachmentService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        try
        {
            var attachment = await _attachmentService.GetByIdAsync(id, cancellationToken);
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var relative = attachment.StoragePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var full = Path.GetFullPath(Path.Combine(webRoot, relative));
            var root = Path.GetFullPath(webRoot);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(full))
                return NotFound();

            return PhysicalFile(full, attachment.ContentType, attachment.FileName);
        }
        catch (BusinessRuleException ex)
        {
            return HandleBusinessRule(ex);
        }
    }

    private ActionResult HandleBusinessRule(BusinessRuleException ex)
    {
        if (string.Equals(ex.Message, LeaveAccessMessages.Denied, StringComparison.Ordinal))
            return Forbid();

        if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { message = ex.Message });
        return BadRequest(new { message = ex.Message });
    }
}

public sealed class CreateLeaveAttachmentForm
{
    public int LeaveRequestId { get; set; }
    public IFormFile? File { get; set; }
}
