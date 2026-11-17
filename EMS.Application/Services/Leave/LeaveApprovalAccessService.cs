using EMS.Application.Services.Authorization;
using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Manager;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveApprovalAccessService : ILeaveApprovalAccessService
{
    /// <summary>Leave admin menu key (matches RBAC seed). Not the self-service "leave" menu.</summary>
    private const string LeaveAdminMenuKey = "leave.admin";

    private static readonly HashSet<string> AdminRoleKeys = new(StringComparer.Ordinal)
    {
        "ADMIN",
        "ADMINISTRATOR",
    };

    private readonly IIdentityContext _identityContext;
    private readonly ILinkedEmployeeService _linkedEmployeeService;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IBaseRepository<Menu> _menuRepository;
    private readonly IManagerTeamAccessService _managerTeamAccess;

    public LeaveApprovalAccessService(
        IIdentityContext identityContext,
        ILinkedEmployeeService linkedEmployeeService,
        IPermissionEvaluator permissionEvaluator,
        IBaseRepository<Menu> menuRepository,
        IManagerTeamAccessService managerTeamAccess)
    {
        _identityContext = identityContext;
        _linkedEmployeeService = linkedEmployeeService;
        _permissionEvaluator = permissionEvaluator;
        _menuRepository = menuRepository;
        _managerTeamAccess = managerTeamAccess;
    }

    public async Task<int> EnsureCanDecideAsync(LeaveRequest request, CancellationToken cancellationToken = default)
    {
        // The reviewer is stored as an Employee FK, so a linked employee record is mandatory.
        var linked = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        if (linked is null)
            throw new BusinessRuleException(LeaveApprovalMessages.ReviewerNotLinked);

        if (linked.OrganizationId != request.OrganizationId)
            throw new BusinessRuleException(LeaveApprovalMessages.Denied);

        if (linked.Id == request.EmployeeId)
            throw new BusinessRuleException(LeaveApprovalMessages.SelfDecisionDenied);

        if (await IsLeaveAdministratorAsync(cancellationToken))
            return linked.Id;

        // Direct manager: current Employee.ManagerId only (former managers do not qualify).
        if (await _managerTeamAccess.CanViewTeamDashboardAsync(cancellationToken)
            && await _managerTeamAccess.IsDirectReportAsync(linked.Id, request.EmployeeId, cancellationToken))
        {
            return linked.Id;
        }

        throw new BusinessRuleException(LeaveApprovalMessages.Denied);
    }

    private async Task<bool> IsLeaveAdministratorAsync(CancellationToken cancellationToken)
    {
        var actor = _identityContext.GetCurrent();
        if (actor.RoleKeys.Any(role => AdminRoleKeys.Contains(role)))
            return true;

        var menuId = await _menuRepository.GetQueryable()
            .AsNoTracking()
            .Where(m => m.Key == LeaveAdminMenuKey)
            .Select(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (menuId == 0)
            return false;

        return await _permissionEvaluator.IsMenuAllowedAsync(
            actor.RoleKeys.ToList(),
            menuId,
            cancellationToken);
    }
}
