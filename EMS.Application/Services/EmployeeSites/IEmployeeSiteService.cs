using EMS.Application.DTOs.Employee;
using EMS.Application.DTOs.Site;

namespace EMS.Application.Services.EmployeeSites;

public interface IEmployeeSiteService
{
    Task<IReadOnlyList<SiteResponseModel>?> GetSitesForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeResponseModel>?> GetEmployeesForSiteAsync(int siteId, CancellationToken cancellationToken = default);

    Task LinkAsync(int siteId, int employeeId, CancellationToken cancellationToken = default);

    Task<bool> UnlinkAsync(int siteId, int employeeId, CancellationToken cancellationToken = default);
}
