using EMS.Application.Services.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeAccessService : IEmployeeAccessService
{
    private const string EmployeesMenuKey = "employees";

    private static readonly HashSet<string> AdminRoleKeys = new(StringComparer.Ordinal)
    {
        "ADMIN",
        "ADMINISTRATOR",
    };

    private readonly IIdentityContext _identityContext;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IBaseRepository<Menu> _menuRepository;

    private int? _employeesMenuId;

    public EmployeeAccessService(
        IIdentityContext identityContext,
        IPermissionEvaluator permissionEvaluator,
        IBaseRepository<Menu> menuRepository)
    {
        _identityContext = identityContext;
        _permissionEvaluator = permissionEvaluator;
        _menuRepository = menuRepository;
    }

    public async Task EnsureCanViewEmployeesAsync(CancellationToken cancellationToken = default)
    {
        if (await CanViewEmployeesAsync(cancellationToken))
            return;

        throw new BusinessRuleException(EmployeeAccessMessages.Denied);
    }

    public async Task EnsureCanManageEmployeesAsync(CancellationToken cancellationToken = default)
    {
        if (await CanManageEmployeesAsync(cancellationToken))
            return;

        throw new BusinessRuleException(EmployeeAccessMessages.Denied);
    }

    private async Task<bool> CanViewEmployeesAsync(CancellationToken cancellationToken)
    {
        if (IsAdmin())
            return true;

        return await HasEmployeesMenuPermissionAsync(cancellationToken);
    }

    private async Task<bool> CanManageEmployeesAsync(CancellationToken cancellationToken)
    {
        if (IsAdmin())
            return true;

        return await HasEmployeesMenuPermissionAsync(cancellationToken);
    }

    private bool IsAdmin()
    {
        var actor = _identityContext.GetCurrent();
        return actor.RoleKeys.Any(role => AdminRoleKeys.Contains(role));
    }

    private async Task<bool> HasEmployeesMenuPermissionAsync(CancellationToken cancellationToken)
    {
        var menuId = await ResolveEmployeesMenuIdAsync(cancellationToken);
        if (menuId is null)
            return false;

        var actor = _identityContext.GetCurrent();
        return await _permissionEvaluator.IsMenuAllowedAsync(
            actor.RoleKeys.ToList(),
            menuId.Value,
            cancellationToken);
    }

    private async Task<int?> ResolveEmployeesMenuIdAsync(CancellationToken cancellationToken)
    {
        if (_employeesMenuId.HasValue)
            return _employeesMenuId;

        var id = await _menuRepository.GetQueryable()
            .AsNoTracking()
            .Where(m => m.Key == EmployeesMenuKey)
            .Select(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (id == 0)
            return null;

        _employeesMenuId = id;
        return _employeesMenuId;
    }
}
