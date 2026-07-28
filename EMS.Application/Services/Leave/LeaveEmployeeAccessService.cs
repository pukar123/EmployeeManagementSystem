using EMS.Application.Services.Authorization;
using EMS.Application.Services.EmployeePortal;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveEmployeeAccessService : ILeaveEmployeeAccessService
{
    /// <summary>Stable menu key for the self-service Leave page (matches RBAC seed).</summary>
    private const string LeaveMenuKey = "leave";

    private readonly IIdentityContext _identityContext;
    private readonly ILinkedEmployeeService _linkedEmployeeService;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IBaseRepository<Menu> _menuRepository;

    private int? _leaveMenuId;

    public LeaveEmployeeAccessService(
        IIdentityContext identityContext,
        ILinkedEmployeeService linkedEmployeeService,
        IPermissionEvaluator permissionEvaluator,
        IBaseRepository<Menu> menuRepository)
    {
        _identityContext = identityContext;
        _linkedEmployeeService = linkedEmployeeService;
        _permissionEvaluator = permissionEvaluator;
        _menuRepository = menuRepository;
    }

    public async Task EnsureCanAccessEmployeeForLeaveAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        if (await CanAccessEmployeeInternalAsync(employeeId, cancellationToken))
            return;

        throw new BusinessRuleException(LeaveAccessMessages.Denied);
    }

    public async Task<bool> CanManageOtherEmployeesLeaveAsync(CancellationToken cancellationToken = default)
    {
        var menuId = await ResolveLeaveMenuIdAsync(cancellationToken);
        if (menuId is null)
            return false;

        var actor = _identityContext.GetCurrent();
        return await _permissionEvaluator.IsMenuAllowedAsync(
            actor.RoleKeys.ToList(),
            menuId.Value,
            cancellationToken);
    }

    private async Task<bool> CanAccessEmployeeInternalAsync(int employeeId, CancellationToken cancellationToken)
    {
        var linked = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        if (linked is not null && linked.Id == employeeId)
            return true;

        return await CanManageOtherEmployeesLeaveAsync(cancellationToken);
    }

    private async Task<int?> ResolveLeaveMenuIdAsync(CancellationToken cancellationToken)
    {
        if (_leaveMenuId.HasValue)
            return _leaveMenuId;

        var id = await _menuRepository.GetQueryable()
            .AsNoTracking()
            .Where(m => m.Key == LeaveMenuKey)
            .Select(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (id == 0)
            return null;

        _leaveMenuId = id;
        return _leaveMenuId;
    }
}
