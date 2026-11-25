using EMS.Application.DTOs.Document;
using EMS.Application.DTOs.Files;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Documents;

public sealed class DocumentService : IDocumentService
{
    /// <summary>Largest page of documents (the same as the employee directory).</summary>
    public const int MaxPageSize = 100;

    public const string PageMessage = "Page must be 1 or greater.";

    public const string PageSizeMessage = "PageSize must be between 1 and 100.";

    private readonly IBaseRepository<Document> _documents;
    private readonly IBaseRepository<DocumentType> _documentTypes;
    private readonly IBaseRepository<Employee> _employees;
    private readonly IBaseRepository<EmployeeDocument> _employeeDocuments;
    private readonly IDocumentAccessService _access;

    public DocumentService(
        IBaseRepository<Document> documents,
        IBaseRepository<DocumentType> documentTypes,
        IBaseRepository<Employee> employees,
        IBaseRepository<EmployeeDocument> employeeDocuments,
        IDocumentAccessService access)
    {
        _documents = documents;
        _documentTypes = documentTypes;
        _employees = employees;
        _employeeDocuments = employeeDocuments;
        _access = access;
    }

    public async Task<IReadOnlyList<DocumentTypeResponseModel>> GetDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _documentTypes.GetQueryable()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
        return types.Select(t => new DocumentTypeResponseModel { Id = t.Id, Name = t.Name, IsActive = t.IsActive }).ToList();
    }

    public async Task<PagedDocumentResponseModel> GetPageAsync(DocumentListQueryModel query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Page < 1)
            throw new BusinessRuleException(PageMessage);
        if (query.PageSize < 1 || query.PageSize > MaxPageSize)
            throw new BusinessRuleException(PageSizeMessage);

        // The access decision comes before any document is queried.
        if (query.EmployeeId is int requestedEmployeeId)
            await _access.EnsureCanViewEmployeeDocumentsAsync(requestedEmployeeId, cancellationToken);
        else
            await _access.EnsureIsDocumentAdminAsync(cancellationToken);

        var scoped = _documents.GetQueryable().AsNoTracking();
        if (query.EmployeeId is int eid)
            scoped = scoped.Where(d => d.EmployeeDocuments.Any(ed => ed.EmployeeId == eid && ed.IsActive && !ed.IsDeleted));
        if (query.DocumentTypeId is int typeId)
            scoped = scoped.Where(d => d.DocumentTypeId == typeId);

        // Count, order and page in the database; only the requested page is loaded.
        var totalCount = await scoped.CountAsync(cancellationToken);
        var page = await scoped
            .OrderByDescending(d => d.UpdatedAtUtc)
            .ThenByDescending(d => d.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(d => d.DocumentType)
            .Include(d => d.EmployeeDocuments.Where(ed => ed.IsActive && !ed.IsDeleted))
                .ThenInclude(ed => ed.Employee)
            .ToListAsync(cancellationToken);

        return new PagedDocumentResponseModel
        {
            Items = page.Select(DocumentMapper.ToResponse).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<DocumentResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _access.EnsureCanViewDocumentAsync(id, cancellationToken))
            return null;

        var entity = await LoadForResponseAsync(id, cancellationToken);
        return entity is null ? null : DocumentMapper.ToResponse(entity);
    }

    public async Task<StoredFileModel?> GetStoredFileAsync(int id, CancellationToken cancellationToken = default)
    {
        // Checked before the stored file is looked at, so a refused caller learns nothing about the file.
        if (!await _access.EnsureCanViewDocumentAsync(id, cancellationToken))
            return null;

        var entity = await _documents.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return entity is null
            ? null
            : new StoredFileModel
            {
                StoragePath = entity.StoredRelativePath,
                ContentType = entity.ContentType,
                FileName = entity.OriginalFileName,
            };
    }

    public async Task<DocumentResponseModel> CreateAsync(
        int? employeeId,
        int documentTypeId,
        string name,
        DateTime? issueDate,
        DateTime? expiryDate,
        string originalFileName,
        string contentType,
        string storedRelativePath,
        DocumentFileKind fileKind,
        CancellationToken cancellationToken = default)
    {
        await _access.EnsureIsDocumentAdminAsync(cancellationToken);

        if (fileKind == DocumentFileKind.None)
            throw new BusinessRuleException("Unsupported or missing file type.");

        if (await _documentTypes.GetByIdAsync(documentTypeId, cancellationToken) is null)
            throw new BusinessRuleException("Document type was not found.");

        if (employeeId is int empId && await _employees.GetByIdAsync(empId, cancellationToken) is null)
            throw new BusinessRuleException("Employee was not found.");

        var now = DateTime.UtcNow;
        var entity = new Document
        {
            DocumentTypeId = documentTypeId,
            Name = name.Trim(),
            IssueDate = issueDate,
            ExpiryDate = expiryDate,
            FileKind = fileKind,
            OriginalFileName = originalFileName.Trim(),
            ContentType = contentType,
            StoredRelativePath = storedRelativePath,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        await using var createTransaction = await _documents.BeginTransactionAsync(cancellationToken);
        try
        {
            await _documents.AddAsync(entity, cancellationToken);

            if (employeeId is int linkedEmployeeId)
            {
                await _employeeDocuments.AddAsync(new EmployeeDocument
                {
                    EmployeeId = linkedEmployeeId,
                    Document = entity,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                }, cancellationToken);
            }

            await _documents.SaveChangesAsync(cancellationToken);
            await createTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await createTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        var reloaded = await LoadForResponseAsync(entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("The new document could not be read back.");
        return DocumentMapper.ToResponse(reloaded);
    }

    public async Task<DocumentResponseModel?> UpdateAsync(int id, UpdateDocumentRequestModel request, CancellationToken cancellationToken = default)
    {
        await _access.EnsureIsDocumentAdminAsync(cancellationToken);

        var entity = await _documents.GetQueryable()
            .Include(d => d.DocumentType)
            .Include(d => d.EmployeeDocuments)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (entity is null)
            return null;

        if (await _documentTypes.GetByIdAsync(request.DocumentTypeId, cancellationToken) is null)
            throw new BusinessRuleException("Document type was not found.");

        if (request.EmployeeId is int empId && await _employees.GetByIdAsync(empId, cancellationToken) is null)
            throw new BusinessRuleException("Employee was not found.");

        entity.Name = request.Name.Trim();
        entity.DocumentTypeId = request.DocumentTypeId;
        entity.IssueDate = request.IssueDate;
        entity.ExpiryDate = request.ExpiryDate;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        var existingLinks = await _employeeDocuments.GetQueryable()
            .Where(ed => ed.DocumentId == id)
            .ToListAsync(cancellationToken);

        foreach (var link in existingLinks)
        {
            link.IsActive = false;
            link.IsDeleted = true;
            link.UpdatedAtUtc = DateTime.UtcNow;
            _employeeDocuments.Update(link);
        }

        if (request.EmployeeId is int employeeId)
        {
            await _employeeDocuments.AddAsync(new EmployeeDocument
            {
                EmployeeId = employeeId,
                DocumentId = id,
                IsActive = true,
                IsDeleted = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            }, cancellationToken);
        }

        await using var updateTransaction = await _documents.BeginTransactionAsync(cancellationToken);
        try
        {
            _documents.Update(entity);
            await _documents.SaveChangesAsync(cancellationToken);
            await updateTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await updateTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        var reloaded = await LoadForResponseAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The updated document could not be read back.");
        return DocumentMapper.ToResponse(reloaded);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _access.EnsureIsDocumentAdminAsync(cancellationToken);

        var entity = await _documents.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;
        _documents.Remove(entity);
        await _documents.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>One document with what its response needs: the type, and the active link with its employee's name.</summary>
    private Task<Document?> LoadForResponseAsync(int id, CancellationToken cancellationToken)
        => _documents.GetQueryable()
            .AsNoTracking()
            .Include(d => d.DocumentType)
            .Include(d => d.EmployeeDocuments.Where(ed => ed.IsActive && !ed.IsDeleted))
                .ThenInclude(ed => ed.Employee)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
}
