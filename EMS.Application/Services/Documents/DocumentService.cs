using EMS.Application.DTOs.Document;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Documents;

public sealed class DocumentService : IDocumentService
{
    private readonly IBaseRepository<Document> _documents;
    private readonly IBaseRepository<DocumentType> _documentTypes;
    private readonly IBaseRepository<Employee> _employees;
    private readonly IBaseRepository<EmployeeDocument> _employeeDocuments;

    public DocumentService(
        IBaseRepository<Document> documents,
        IBaseRepository<DocumentType> documentTypes,
        IBaseRepository<Employee> employees,
        IBaseRepository<EmployeeDocument> employeeDocuments)
    {
        _documents = documents;
        _documentTypes = documentTypes;
        _employees = employees;
        _employeeDocuments = employeeDocuments;
    }

    public async Task<IReadOnlyList<DocumentTypeResponseModel>> GetDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _documentTypes.GetQueryable()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
        return types.Select(t => new DocumentTypeResponseModel { Id = t.Id, Name = t.Name, IsActive = t.IsActive }).ToList();
    }

    public async Task<IReadOnlyList<DocumentResponseModel>> GetAllAsync(int? employeeId, CancellationToken cancellationToken = default)
    {
        var q = _documents.GetQueryable()
            .Include(d => d.DocumentType)
            .Include(d => d.EmployeeDocuments)
            .AsQueryable();
        if (employeeId is int eid)
            q = q.Where(d => d.EmployeeDocuments.Any(ed => ed.EmployeeId == eid && ed.IsActive && !ed.IsDeleted));
        var list = await q.OrderByDescending(d => d.UpdatedAtUtc).ToListAsync(cancellationToken);
        return list.Select(DocumentMapper.ToResponse).ToList();
    }

    public async Task<DocumentResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _documents.GetQueryable()
            .Include(d => d.DocumentType)
            .Include(d => d.EmployeeDocuments)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return entity is null ? null : DocumentMapper.ToResponse(entity);
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

        await _documents.AddAsync(entity, cancellationToken);
        await _documents.SaveChangesAsync(cancellationToken);

        if (employeeId is int linkedEmployeeId)
        {
            var linkNow = DateTime.UtcNow;
            await _employeeDocuments.AddAsync(new EmployeeDocument
            {
                EmployeeId = linkedEmployeeId,
                DocumentId = entity.Id,
                IsActive = true,
                IsDeleted = false,
                CreatedAtUtc = linkNow,
                UpdatedAtUtc = linkNow,
            }, cancellationToken);
            await _employeeDocuments.SaveChangesAsync(cancellationToken);
        }

        var reloaded = await _documents.GetQueryable()
            .Include(d => d.DocumentType)
            .Include(d => d.EmployeeDocuments)
            .FirstAsync(d => d.Id == entity.Id, cancellationToken);
        return DocumentMapper.ToResponse(reloaded);
    }

    public async Task<DocumentResponseModel?> UpdateAsync(int id, UpdateDocumentRequestModel request, CancellationToken cancellationToken = default)
    {
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

        _documents.Update(entity);
        await _documents.SaveChangesAsync(cancellationToken);
        await _employeeDocuments.SaveChangesAsync(cancellationToken);

        var reloaded = await _documents.GetQueryable()
            .Include(d => d.DocumentType)
            .Include(d => d.EmployeeDocuments)
            .FirstAsync(d => d.Id == id, cancellationToken);
        return DocumentMapper.ToResponse(reloaded);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _documents.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;
        _documents.Remove(entity);
        await _documents.SaveChangesAsync(cancellationToken);
        return true;
    }
}
