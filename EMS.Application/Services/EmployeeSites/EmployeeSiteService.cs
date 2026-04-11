using EMS.Application.DTOs.Employee;
using EMS.Application.DTOs.Site;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.EmployeeSites;

public sealed class EmployeeSiteService : IEmployeeSiteService
{
    private readonly IBaseRepository<EmployeeSite> _employeeSiteRepository;
    private readonly IBaseRepository<Employee> _employeeRepository;
    private readonly IBaseRepository<Site> _siteRepository;

    public EmployeeSiteService(
        IBaseRepository<EmployeeSite> employeeSiteRepository,
        IBaseRepository<Employee> employeeRepository,
        IBaseRepository<Site> siteRepository)
    {
        _employeeSiteRepository = employeeSiteRepository;
        _employeeRepository = employeeRepository;
        _siteRepository = siteRepository;
    }

    public async Task<IReadOnlyList<SiteResponseModel>?> GetSitesForEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
            return null;

        var sites = await (
            from es in _employeeSiteRepository.GetQueryable()
            join s in _siteRepository.GetQueryable() on es.SiteId equals s.SiteId
            where es.EmployeeId == employeeId && !s.IsDeleted
            orderby s.SiteName
            select s).ToListAsync(cancellationToken);

        return sites.Select(SiteMapper.ToResponse).ToList();
    }

    public async Task<IReadOnlyList<EmployeeResponseModel>?> GetEmployeesForSiteAsync(int siteId, CancellationToken cancellationToken = default)
    {
        var site = await _siteRepository.GetByIdAsync(siteId, cancellationToken);
        if (site is null || site.IsDeleted)
            return null;

        var employees = await (
            from es in _employeeSiteRepository.GetQueryable()
            join e in _employeeRepository.GetQueryable() on es.EmployeeId equals e.Id
            where es.SiteId == siteId
            orderby e.LastName, e.FirstName
            select e).ToListAsync(cancellationToken);

        return employees.Select(EmployeeMapper.ToResponse).ToList();
    }

    public async Task LinkAsync(int siteId, int employeeId, CancellationToken cancellationToken = default)
    {
        var site = await _siteRepository.GetByIdAsync(siteId, cancellationToken);
        if (site is null || site.IsDeleted)
            throw new BusinessRuleException("Site was not found.");

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
            throw new BusinessRuleException("Employee was not found.");

        var exists = await _employeeSiteRepository.GetQueryable()
            .AnyAsync(es => es.SiteId == siteId && es.EmployeeId == employeeId, cancellationToken);
        if (exists)
            throw new BusinessRuleException("This employee is already linked to the site.");

        await _employeeSiteRepository.AddAsync(new EmployeeSite
        {
            EmployeeId = employeeId,
            SiteId = siteId
        }, cancellationToken);
        await _employeeSiteRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UnlinkAsync(int siteId, int employeeId, CancellationToken cancellationToken = default)
    {
        var link = await _employeeSiteRepository.GetQueryable()
            .FirstOrDefaultAsync(es => es.SiteId == siteId && es.EmployeeId == employeeId, cancellationToken);
        if (link is null)
            return false;

        _employeeSiteRepository.Remove(link);
        await _employeeSiteRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
