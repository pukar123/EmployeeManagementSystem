using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Manager;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Documents;

public sealed class DocumentAccessService : IDocumentAccessService
{
    private readonly ILinkedEmployeeService _linkedEmployeeService;
    private readonly IDocumentAdminAccessService _documentAdminAccess;
    private readonly IManagerTeamAccessService _managerTeamAccess;
    private readonly IBaseRepository<Document> _documents;

    public DocumentAccessService(
        ILinkedEmployeeService linkedEmployeeService,
        IDocumentAdminAccessService documentAdminAccess,
        IManagerTeamAccessService managerTeamAccess,
        IBaseRepository<Document> documents)
    {
        _linkedEmployeeService = linkedEmployeeService;
        _documentAdminAccess = documentAdminAccess;
        _managerTeamAccess = managerTeamAccess;
        _documents = documents;
    }

    public async Task EnsureIsDocumentAdminAsync(CancellationToken cancellationToken = default)
    {
        if (!await _documentAdminAccess.IsDocumentAdminAsync(cancellationToken))
            throw new BusinessRuleException(DocumentAccessMessages.Denied);
    }

    public async Task EnsureCanViewEmployeeDocumentsAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        if (await _documentAdminAccess.IsDocumentAdminAsync(cancellationToken))
            return;

        var linked = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        if (linked is null)
            throw new BusinessRuleException(DocumentAccessMessages.Denied);

        if (linked.Id == employeeId)
            return;

        // Direct manager: read-only, current Employee.ManagerId only (former managers and archived reports excluded).
        // An id that does not exist is never a direct report, so ids cannot be probed.
        if (await _managerTeamAccess.CanViewTeamDashboardAsync(cancellationToken)
            && await _managerTeamAccess.IsDirectReportAsync(linked.Id, employeeId, cancellationToken))
        {
            return;
        }

        throw new BusinessRuleException(DocumentAccessMessages.Denied);
    }

    public async Task<bool> EnsureCanViewDocumentAsync(int documentId, CancellationToken cancellationToken = default)
    {
        var isDocumentAdmin = await _documentAdminAccess.IsDocumentAdminAsync(cancellationToken);

        var row = await _documents.GetQueryable()
            .AsNoTracking()
            .Where(d => d.Id == documentId)
            .Select(d => new
            {
                OwnerId = d.EmployeeDocuments
                    .Where(ed => ed.IsActive && !ed.IsDeleted)
                    .Select(ed => (int?)ed.EmployeeId)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            if (isDocumentAdmin)
                return false;

            throw new BusinessRuleException(DocumentAccessMessages.Denied);
        }

        if (isDocumentAdmin)
            return true;

        // A document with no active owner is for document admins only.
        if (row.OwnerId is not int ownerId)
            throw new BusinessRuleException(DocumentAccessMessages.Denied);

        await EnsureCanViewEmployeeDocumentsAsync(ownerId, cancellationToken);
        return true;
    }
}
