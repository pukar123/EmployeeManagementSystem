using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Manager;
using EMS.Domain.DbModels;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveApprovalAccessService : ILeaveApprovalAccessService
{
    private readonly ILinkedEmployeeService _linkedEmployeeService;
    private readonly IManagerTeamAccessService _managerTeamAccess;
    private readonly ILeaveAdminAccessService _leaveAdminAccess;

    public LeaveApprovalAccessService(
        ILinkedEmployeeService linkedEmployeeService,
        IManagerTeamAccessService managerTeamAccess,
        ILeaveAdminAccessService leaveAdminAccess)
    {
        _linkedEmployeeService = linkedEmployeeService;
        _managerTeamAccess = managerTeamAccess;
        _leaveAdminAccess = leaveAdminAccess;
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

        if (await _leaveAdminAccess.IsLeaveAdminAsync(cancellationToken))
            return linked.Id;

        // Direct manager: current Employee.ManagerId only (former managers do not qualify).
        if (await _managerTeamAccess.CanViewTeamDashboardAsync(cancellationToken)
            && await _managerTeamAccess.IsDirectReportAsync(linked.Id, request.EmployeeId, cancellationToken))
        {
            return linked.Id;
        }

        throw new BusinessRuleException(LeaveApprovalMessages.Denied);
    }
}
