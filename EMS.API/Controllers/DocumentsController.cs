using EMS.Application.DTOs.Document;
using EMS.Application.Services.Documents;
using EMS.API.Services;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;
    private readonly LocalDocumentFileStorage _storage;
    private readonly IWebHostEnvironment _env;

    public DocumentsController(
        IDocumentService documents,
        LocalDocumentFileStorage storage,
        IWebHostEnvironment env)
    {
        _documents = documents;
        _storage = storage;
        _env = env;
    }

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<DocumentTypeResponseModel>>> GetDocumentTypes(CancellationToken cancellationToken)
    {
        var items = await _documents.GetDocumentTypesAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentResponseModel>>> GetAll(
        [FromQuery] int? employeeId,
        CancellationToken cancellationToken)
    {
        var items = await _documents.GetAllAsync(employeeId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DocumentResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _documents.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Upload a document (PDF, Word, or image). Metadata via form fields.</summary>
    [HttpPost]
    [RequestSizeLimit(16 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 16 * 1024 * 1024)]
    public async Task<ActionResult<DocumentResponseModel>> Create(
        [FromForm] CreateDocumentForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest(new { message = "A file is required." });

        if (string.IsNullOrWhiteSpace(form.Name))
            return BadRequest(new { message = "Name is required." });

        string? relativePath = null;
        try
        {
            LocalDocumentFileStorage.ValidateContentOrThrow(form.File);
            var savedFile = await _storage.SaveAsync(form.EmployeeId, form.File, cancellationToken);
            relativePath = savedFile.RelativePath;
            var contentType = savedFile.ContentType;
            var kind = savedFile.Kind;

            var issue = ParseOptionalDate(form.IssueDate);
            var expiry = ParseOptionalDate(form.ExpiryDate);

            var created = await _documents.CreateAsync(
                form.EmployeeId,
                form.DocumentTypeId,
                form.Name.Trim(),
                issue,
                expiry,
                form.File.FileName,
                contentType,
                relativePath,
                kind,
                cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DocumentResponseModel>> Update(
        int id,
        [FromBody] UpdateDocumentRequestModel request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _documents.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await _documents.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        _storage.TryDelete(existing.StoredRelativePath);
        var deleted = await _documents.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var doc = await _documents.GetByIdAsync(id, cancellationToken);
        if (doc is null)
            return NotFound();

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var relative = doc.StoredRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(webRoot, relative));
        var root = Path.GetFullPath(webRoot);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(full))
            return NotFound();

        return PhysicalFile(full, doc.ContentType, doc.OriginalFileName);
    }

    private static DateTime? ParseOptionalDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTime.TryParse(value, out var d) ? DateTime.SpecifyKind(d.Date, DateTimeKind.Utc) : null;
    }
}

/// <summary>Multipart form for POST /api/Documents.</summary>
public sealed class CreateDocumentForm
{
    public int? EmployeeId { get; set; }

    public int DocumentTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Optional ISO date string (e.g. yyyy-MM-dd).</summary>
    public string? IssueDate { get; set; }

    public string? ExpiryDate { get; set; }

    public IFormFile? File { get; set; }
}
