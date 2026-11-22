using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Manager;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveEmployeeAccessService : ILeaveEmployeeAccessService
{
    private readonly ILinkedEmployeeService _linkedEmployeeService;
    private readonly ILeaveAdminAccessService _leaveAdminAccess;
    private readonly IManagerTeamAccessService _managerTeamAccess;

    public LeaveEmployeeAccessService(
        ILinkedEmployeeService linkedEmployeeService,
        ILeaveAdminAccessService leaveAdminAccess,
        IManagerTeamAccessService managerTeamAccess)
    {
        _linkedEmployeeService = linkedEmployeeService;
        _leaveAdminAccess = leaveAdminAccess;
        _managerTeamAccess = managerTeamAccess;
    }

    public async Task EnsureCanViewEmployeeLeaveAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var linkedEmployeeId = await GetLinkedEmployeeIdAsync(cancellationToken);
        if (linkedEmployeeId == employeeId)
            return;

        if (await _leaveAdminAccess.IsLeaveAdminAsync(cancellationToken))
            return;

        // Direct manager: read-only, current Employee.ManagerId only (former managers and archived reports excluded).
        if (linkedEmployeeId is not null
            && await _managerTeamAccess.CanViewTeamDashboardAsync(cancellationToken)
            && await _managerTeamAccess.IsDirectReportAsync(linkedEmployeeId.Value, employeeId, cancellationToken))
        {
            return;
        }

        throw new BusinessRuleException(LeaveAccessMessages.Denied);
    }

    public Task EnsureCanManageEmployeeLeaveAsync(int employeeId, CancellationToken cancellationToken = default)
        => EnsureSelfOrLeaveAdminAsync(employeeId, cancellationToken);

    public Task EnsureCanAccessLeaveAttachmentsAsync(int employeeId, CancellationToken cancellationToken = default)
        => EnsureSelfOrLeaveAdminAsync(employeeId, cancellationToken);

    public Task<bool> CanManageOtherEmployeesLeaveAsync(CancellationToken cancellationToken = default)
        => _leaveAdminAccess.IsLeaveAdminAsync(cancellationToken);

    private async Task EnsureSelfOrLeaveAdminAsync(int employeeId, CancellationToken cancellationToken)
    {
        if (await GetLinkedEmployeeIdAsync(cancellationToken) == employeeId)
            return;

        if (await _leaveAdminAccess.IsLeaveAdminAsync(cancellationToken))
            return;

        throw new BusinessRuleException(LeaveAccessMessages.Denied);
    }

    private async Task<int?> GetLinkedEmployeeIdAsync(CancellationToken cancellationToken)
    {
        var linked = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        return linked?.Id;
    }
}
