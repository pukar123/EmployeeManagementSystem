using System.Globalization;
using System.Text;
using EMS.Application.DTOs.Employee;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeDirectoryService : IEmployeeDirectoryService
{
    private const int MaxPageSize = 100;
    private const int MaxExportRows = 10_000;

    private readonly IBaseRepository<Employee> _employees;
    private readonly IEmployeeUserManagementGateway _gateway;

    public EmployeeDirectoryService(
        IBaseRepository<Employee> employees,
        IEmployeeUserManagementGateway gateway)
    {
        _employees = employees;
        _gateway = gateway;
    }

    public async Task<PagedEmployeeDirectoryResponseModel> QueryAsync(
        EmployeeDirectoryQueryModel query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        var normalized = NormalizeQuery(query);

        if (RequiresLoginLinkResolution(normalized.LoginLinkStatus))
        {
            var allMatching = await BuildDirectoryItemsAsync(normalized, cancellationToken);
            var filtered = FilterByLoginLinkStatus(allMatching, normalized.LoginLinkStatus!);
            return PaginateInMemory(filtered, normalized.Page, normalized.PageSize);
        }

        var baseQuery = BuildFilteredQuery(normalized);
        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var page = normalized.Page;
        var pageSize = normalized.PageSize;
        var sorted = ApplySorting(baseQuery, normalized.SortBy, normalized.SortDirection);
        var pageEntities = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await MapToDirectoryItemsAsync(pageEntities, cancellationToken);
        return new PagedEmployeeDirectoryResponseModel
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<EmployeeExportFileResponseModel> ExportCsvAsync(
        EmployeeDirectoryQueryModel query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        var normalized = NormalizeQuery(query);
        normalized.Page = 1;
        normalized.PageSize = MaxExportRows;

        IReadOnlyList<EmployeeDirectoryItemResponseModel> items;
        if (RequiresLoginLinkResolution(normalized.LoginLinkStatus))
        {
            var allMatching = await BuildDirectoryItemsAsync(normalized, cancellationToken);
            items = FilterByLoginLinkStatus(allMatching, normalized.LoginLinkStatus!);
        }
        else
        {
            var baseQuery = BuildFilteredQuery(normalized);
            var sorted = ApplySorting(baseQuery, normalized.SortBy, normalized.SortDirection);
            var entities = await sorted.Take(MaxExportRows).ToListAsync(cancellationToken);
            items = await MapToDirectoryItemsAsync(entities, cancellationToken);
        }

        var sb = new StringBuilder();
        sb.AppendLine("EmployeeNumber,FirstName,LastName,Email,EmploymentStatus,IsActive,IsArchived,Department,Position,Manager,PrimarySite,LinkedLogin,LoginActive");
        foreach (var row in items)
        {
            sb.Append(Csv(row.EmployeeNumber));
            sb.Append(',');
            sb.Append(Csv(row.FirstName));
            sb.Append(',');
            sb.Append(Csv(row.LastName));
            sb.Append(',');
            sb.Append(Csv(row.Email));
            sb.Append(',');
            sb.Append(Csv(row.EmploymentStatus.ToString()));
            sb.Append(',');
            sb.Append(row.IsActive ? "true" : "false");
            sb.Append(',');
            sb.Append(row.IsArchived ? "true" : "false");
            sb.Append(',');
            sb.Append(Csv(row.DepartmentName));
            sb.Append(',');
            sb.Append(Csv(row.JobPositionTitle));
            sb.Append(',');
            sb.Append(Csv(row.ManagerName));
            sb.Append(',');
            sb.Append(Csv(row.PrimarySiteName));
            sb.Append(',');
            sb.Append(row.HasLinkedLogin ? "true" : "false");
            sb.Append(',');
            sb.Append(row.LinkedLoginIsActive.HasValue ? (row.LinkedLoginIsActive.Value ? "true" : "false") : "");
            sb.AppendLine();
        }

        var content = Encoding.UTF8.GetBytes(sb.ToString());
        return new EmployeeExportFileResponseModel
        {
            FileName = $"employees-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv",
            ContentType = "text/csv",
            Content = content,
        };
    }

    private async Task<List<EmployeeDirectoryItemResponseModel>> BuildDirectoryItemsAsync(
        EmployeeDirectoryQueryModel normalized,
        CancellationToken cancellationToken)
    {
        var baseQuery = BuildFilteredQuery(normalized);
        if (normalized.LoginLinkStatus is "linked" or "disabled")
            baseQuery = baseQuery.Where(e => e.ExternalIdentityKey != null);

        var entities = await ApplySorting(baseQuery, normalized.SortBy, normalized.SortDirection)
            .Take(MaxExportRows)
            .ToListAsync(cancellationToken);

        return await MapToDirectoryItemsAsync(entities, cancellationToken);
    }

    private static PagedEmployeeDirectoryResponseModel PaginateInMemory(
        List<EmployeeDirectoryItemResponseModel> items,
        int page,
        int pageSize)
    {
        var totalCount = items.Count;
        var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedEmployeeDirectoryResponseModel
        {
            Items = pageItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    private static bool RequiresLoginLinkResolution(string? loginLinkStatus)
    {
        var normalized = loginLinkStatus?.Trim().ToLowerInvariant();
        return normalized is "linked" or "disabled";
    }

    private static List<EmployeeDirectoryItemResponseModel> FilterByLoginLinkStatus(
        List<EmployeeDirectoryItemResponseModel> items,
        string loginLinkStatus)
    {
        return loginLinkStatus.Trim().ToLowerInvariant() switch
        {
            "linked" => items.Where(x => x.HasLinkedLogin && x.LinkedLoginIsActive == true).ToList(),
            "disabled" => items.Where(x => x.HasLinkedLogin && x.LinkedLoginIsActive == false).ToList(),
            "not_linked" => items.Where(x => !x.HasLinkedLogin).ToList(),
            _ => items,
        };
    }

    private IQueryable<Employee> BuildFilteredQuery(EmployeeDirectoryQueryModel query)
    {
        var q = _employees.GetQueryable()
            .AsNoTracking()
            .Where(e => e.OrganizationId == query.OrganizationId && e.IsArchived == query.IsArchived);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            q = q.Where(e =>
                e.EmployeeNumber.ToUpper().Contains(term)
                || e.FirstName.ToUpper().Contains(term)
                || e.LastName.ToUpper().Contains(term)
                || (e.FirstName + " " + e.LastName).ToUpper().Contains(term)
                || e.Email.ToUpper().Contains(term)
                || (e.PhoneNumber != null && e.PhoneNumber.ToUpper().Contains(term)));
        }

        if (query.EmploymentStatus.HasValue)
            q = q.Where(e => e.EmploymentStatus == query.EmploymentStatus.Value);

        if (query.DepartmentId.HasValue)
            q = q.Where(e => e.DepartmentId == query.DepartmentId.Value);

        if (query.JobPositionId.HasValue)
            q = q.Where(e => e.JobPositionId == query.JobPositionId.Value);

        if (query.ManagerId.HasValue)
            q = q.Where(e => e.ManagerId == query.ManagerId.Value);

        if (query.SiteId.HasValue)
        {
            var siteId = query.SiteId.Value;
            q = q.Where(e => e.EmployeeSites.Any(es => es.SiteId == siteId));
        }

        var loginStatus = query.LoginLinkStatus?.Trim().ToLowerInvariant();
        if (loginStatus == "not_linked")
            q = q.Where(e => e.ExternalIdentityKey == null);

        return q;
    }

    private static IQueryable<Employee> ApplySorting(IQueryable<Employee> query, string sortBy, string sortDirection)
    {
        var desc = sortDirection.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.Trim().ToLowerInvariant() switch
        {
            "employeenumber" => desc
                ? query.OrderByDescending(e => e.EmployeeNumber)
                : query.OrderBy(e => e.EmployeeNumber),
            "datejoined" => desc
                ? query.OrderByDescending(e => e.DateJoined)
                : query.OrderBy(e => e.DateJoined),
            "employmentstatus" => desc
                ? query.OrderByDescending(e => e.EmploymentStatus)
                : query.OrderBy(e => e.EmploymentStatus),
            _ => desc
                ? query.OrderByDescending(e => e.LastName).ThenByDescending(e => e.FirstName)
                : query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName),
        };
    }

    private async Task<List<EmployeeDirectoryItemResponseModel>> MapToDirectoryItemsAsync(
        List<Employee> entities,
        CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
            return [];

        var employeeIds = entities.Select(e => e.Id).ToList();

        var enriched = await _employees.GetQueryable()
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .Include(e => e.Department)
            .Include(e => e.JobPosition)
            .Include(e => e.Manager)
            .Include(e => e.EmployeeSites)
            .ThenInclude(es => es.Site)
            .ToListAsync(cancellationToken);

        var order = employeeIds
            .Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index);

        enriched.Sort((a, b) => order[a.Id].CompareTo(order[b.Id]));

        var results = new List<EmployeeDirectoryItemResponseModel>();
        foreach (var entity in enriched)
        {
            var linked = await ResolveLinkedLoginAsync(entity, cancellationToken);
            results.Add(new EmployeeDirectoryItemResponseModel
            {
                Id = entity.Id,
                EmployeeNumber = entity.EmployeeNumber,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Email = entity.Email,
                EmploymentStatus = entity.EmploymentStatus,
                IsActive = entity.IsActive,
                IsArchived = entity.IsArchived,
                DepartmentId = entity.DepartmentId,
                DepartmentName = entity.Department?.Name,
                JobPositionId = entity.JobPositionId,
                JobPositionTitle = entity.JobPosition?.Title,
                ManagerId = entity.ManagerId,
                ManagerName = entity.Manager is null ? null : $"{entity.Manager.FirstName} {entity.Manager.LastName}",
                ManagerEmployeeNumber = entity.Manager?.EmployeeNumber,
                PrimarySiteName = entity.EmployeeSites
                    .Select(es => es.Site)
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.SiteName)
                    .Select(s => s.SiteName)
                    .FirstOrDefault(),
                HasLinkedLogin = linked.HasLogin,
                LinkedLoginIsActive = linked.IsActive,
            });
        }

        return results;
    }

    private async Task<(bool HasLogin, bool? IsActive)> ResolveLinkedLoginAsync(
        Employee entity,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entity.ExternalIdentityKey))
            return (false, null);

        var user = await EmployeeLinkedIdentityHelper.TryResolveLinkedUserAsync(entity, _gateway, cancellationToken);
        if (user is null)
            return (true, null);

        return (true, user.IsActive);
    }

    private static void ValidateQuery(EmployeeDirectoryQueryModel query)
    {
        if (query.OrganizationId <= 0)
            throw new BusinessRuleException("OrganizationId is required.");

        if (query.Page < 1)
            throw new BusinessRuleException("Page must be at least 1.");

        if (query.PageSize < 1 || query.PageSize > MaxPageSize)
            throw new BusinessRuleException($"PageSize must be between 1 and {MaxPageSize}.");
    }

    private static EmployeeDirectoryQueryModel NormalizeQuery(EmployeeDirectoryQueryModel query)
    {
        query.Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        query.SortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "name" : query.SortBy.Trim();
        query.SortDirection = string.IsNullOrWhiteSpace(query.SortDirection) ? "asc" : query.SortDirection.Trim();
        query.LoginLinkStatus = string.IsNullOrWhiteSpace(query.LoginLinkStatus)
            ? null
            : query.LoginLinkStatus.Trim().ToLowerInvariant();

        if (query.LoginLinkStatus is not null
            and not ("linked" or "not_linked" or "disabled"))
        {
            throw new BusinessRuleException("LoginLinkStatus must be linked, not_linked, or disabled.");
        }

        return query;
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return value;
    }
}
