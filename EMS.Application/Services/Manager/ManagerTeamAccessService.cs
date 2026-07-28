using EMS.Application.Services.Authorization;
using EMS.Application.Services.EmployeePortal;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Manager;

public sealed class ManagerTeamAccessService : IManagerTeamAccessService
{
    private const string ManagerTeamMenuKey = "manager.team";

    private static readonly HashSet<string> AdminRoleKeys = new(StringComparer.Ordinal)
    {
        "ADMIN",
        "ADMINISTRATOR",
    };

    private readonly IIdentityContext _identityContext;
    private readonly ILinkedEmployeeService _linkedEmployeeService;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IBaseRepository<Menu> _menuRepository;
    private readonly IBaseRepository<Employee> _employeeRepository;

    private int? _managerTeamMenuId;

    public ManagerTeamAccessService(
        IIdentityContext identityContext,
        ILinkedEmployeeService linkedEmployeeService,
        IPermissionEvaluator permissionEvaluator,
        IBaseRepository<Menu> menuRepository,
        IBaseRepository<Employee> employeeRepository)
    {
        _identityContext = identityContext;
        _linkedEmployeeService = linkedEmployeeService;
        _permissionEvaluator = permissionEvaluator;
        _menuRepository = menuRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<int> ResolveEffectiveManagerIdAsync(
        int organizationId,
        int? managerId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId <= 0)
            throw new BusinessRuleException("Organization id must be greater than zero.");

        if (IsAdmin())
        {
            if (!managerId.HasValue || managerId.Value <= 0)
                throw new BusinessRuleException(ManagerTeamAccessMessages.Denied);

            await EnsureManagerExistsAsync(organizationId, managerId.Value, cancellationToken);
            return managerId.Value;
        }

        var linked = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        if (linked is null)
            throw new BusinessRuleException(ManagerTeamAccessMessages.Denied);

        if (!await CanViewTeamDashboardAsync(cancellationToken))
            throw new BusinessRuleException(ManagerTeamAccessMessages.Denied);

        if (managerId.HasValue && managerId.Value != linked.Id)
            throw new BusinessRuleException(ManagerTeamAccessMessages.Denied);

        if (linked.OrganizationId != organizationId)
            throw new BusinessRuleException(ManagerTeamAccessMessages.Denied);

        return linked.Id;
    }

    public async Task<bool> CanViewTeamDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (IsAdmin())
            return true;

        var menuId = await ResolveManagerTeamMenuIdAsync(cancellationToken);
        if (menuId is null)
            return false;

        var linked = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        if (linked is null)
            return false;

        var actor = _identityContext.GetCurrent();
        return await _permissionEvaluator.IsMenuAllowedAsync(
            actor.RoleKeys.ToList(),
            menuId.Value,
            cancellationToken);
    }

    public async Task<bool> IsDirectReportAsync(
        int managerEmployeeId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        if (managerEmployeeId <= 0 || employeeId <= 0)
            return false;

        return await _employeeRepository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(
                e => e.Id == employeeId
                     && !e.IsArchived
                     && e.ManagerId == managerEmployeeId,
                cancellationToken);
    }

    private async Task EnsureManagerExistsAsync(
        int organizationId,
        int managerId,
        CancellationToken cancellationToken)
    {
        var exists = await _employeeRepository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(
                e => e.Id == managerId
                     && e.OrganizationId == organizationId
                     && !e.IsArchived,
                cancellationToken);

        if (!exists)
            throw new BusinessRuleException("Manager was not found.");
    }

    private bool IsAdmin()
    {
        var actor = _identityContext.GetCurrent();
        return actor.RoleKeys.Any(role => AdminRoleKeys.Contains(role));
    }

    private async Task<int?> ResolveManagerTeamMenuIdAsync(CancellationToken cancellationToken)
    {
        if (_managerTeamMenuId.HasValue)
            return _managerTeamMenuId;

        var id = await _menuRepository.GetQueryable()
            .AsNoTracking()
            .Where(m => m.Key == ManagerTeamMenuKey)
            .Select(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (id == 0)
            return null;

        _managerTeamMenuId = id;
        return _managerTeamMenuId;
    }
}
