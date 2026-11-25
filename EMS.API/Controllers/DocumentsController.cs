using EMS.API.Auth;
using EMS.Application.DTOs.Document;
using EMS.Application.DTOs.Files;
using EMS.Application.Services.Documents;
using EMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pukar.Shared;

namespace EMS.API.Controllers;

/// <summary>
/// Employee documents (B09). Reads (list, get, download) are for the employee, their current direct manager and
/// document admins, decided in the Application layer; an access denial is 403. Uploads, changes and deletes need the
/// <see cref="AuthorizationPolicyNames.DocumentAdmin"/> policy, which runs before the action, so a refused upload
/// never writes a file. Document types stay readable by every signed-in user.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;
    private readonly LocalDocumentFileStorage _storage;
    private readonly PrivateFileStore _store;

    public DocumentsController(
        IDocumentService documents,
        LocalDocumentFileStorage storage,
        PrivateFileStore store)
    {
        _documents = documents;
        _storage = storage;
        _store = store;
    }

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<DocumentTypeResponseModel>>> GetDocumentTypes(CancellationToken cancellationToken)
    {
        var items = await _documents.GetDocumentTypesAsync(cancellationToken);
        return Ok(items);
    }

    /// <summary>One page of documents. Without <c>employeeId</c>: every document, for document admins only.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedDocumentResponseModel>> GetAll(
        [FromQuery] DocumentListQueryModel query,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = await _documents.GetPageAsync(query, cancellationToken);
            return Ok(page);
        }
        catch (BusinessRuleException ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DocumentResponseModel>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _documents.GetByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (BusinessRuleException ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>Upload a document (PDF, Word, or image). Metadata via form fields.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.DocumentAdmin)]
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
            relativePath = savedFile.StorageKey;
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
            return HandleError(ex);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.DocumentAdmin)]
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
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.DocumentAdmin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var stored = await _documents.GetStoredFileAsync(id, cancellationToken);
            if (stored is null)
                return NotFound();

            // The service checks document admin again before the row is removed. Checked here first so that a
            // refused call never removes the file.
            var deleted = await _documents.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return NotFound();

            // An unsafe or legacy path deletes nothing on disk; the row is removed either way.
            _storage.TryDelete(stored.StoragePath);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        StoredFileModel? stored;
        try
        {
            // Access is checked before the file is resolved, so a refused caller learns nothing about the file.
            stored = await _documents.GetStoredFileAsync(id, cancellationToken);
        }
        catch (BusinessRuleException ex)
        {
            return HandleError(ex);
        }

        if (stored is null || !_store.TryResolve(stored.StoragePath, out var full) || !System.IO.File.Exists(full))
            return NotFound();

        return PhysicalFile(full, FileDownloadHeaders.SafeContentType(stored.ContentType), FileDownloadHeaders.SafeFileName(stored.FileName));
    }

    /// <summary>403 for a document access denial (checked first), otherwise 400 as before.</summary>
    private ActionResult HandleError(BusinessRuleException ex)
    {
        if (string.Equals(ex.Message, DocumentAccessMessages.Denied, StringComparison.Ordinal))
            return Forbid();

        return BadRequest(new { message = ex.Message });
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
