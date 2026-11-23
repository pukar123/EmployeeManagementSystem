using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Manager;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Attendance;

public sealed class AttendanceEmployeeAccessService : IAttendanceEmployeeAccessService
{
    private readonly ILinkedEmployeeService _linkedEmployeeService;
    private readonly IAttendanceAdminAccessService _attendanceAdminAccess;
    private readonly IManagerTeamAccessService _managerTeamAccess;
    private readonly IBaseRepository<Employee> _employeeRepository;

    public AttendanceEmployeeAccessService(
        ILinkedEmployeeService linkedEmployeeService,
        IAttendanceAdminAccessService attendanceAdminAccess,
        IManagerTeamAccessService managerTeamAccess,
        IBaseRepository<Employee> employeeRepository)
    {
        _linkedEmployeeService = linkedEmployeeService;
        _attendanceAdminAccess = attendanceAdminAccess;
        _managerTeamAccess = managerTeamAccess;
        _employeeRepository = employeeRepository;
    }

    public async Task<AttendanceActionTarget> ResolveActionTargetAsync(
        int? requestedEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var requested = requestedEmployeeId is > 0 ? requestedEmployeeId : null;
        var linked = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        var isAttendanceAdmin = await _attendanceAdminAccess.IsAttendanceAdminAsync(cancellationToken);

        if (requested is null || requested == linked?.Id)
        {
            if (linked is not null)
                return new AttendanceActionTarget(linked.Id, isAttendanceAdmin);

            // Unlinked: nobody can be derived. An admin page may be used by an unlinked admin, who must pick someone.
            if (isAttendanceAdmin)
                throw new BusinessRuleException(AttendanceAccessMessages.ChooseEmployee);

            throw new BusinessRuleException(AttendanceAccessMessages.Denied);
        }

        // Another employee: refused rather than silently redirected to the caller (see B07 requirements 3.1).
        if (isAttendanceAdmin)
            return new AttendanceActionTarget(requested.Value, MayUseClientTime: true);

        throw new BusinessRuleException(AttendanceAccessMessages.Denied);
    }

    public async Task EnsureIsAttendanceAdminAsync(CancellationToken cancellationToken = default)
    {
        if (!await _attendanceAdminAccess.IsAttendanceAdminAsync(cancellationToken))
            throw new BusinessRuleException(AttendanceAccessMessages.Denied);
    }

    public async Task EnsureCanViewEmployeeAttendanceAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var linked = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        if (linked is not null && linked.Id == employeeId)
            return;

        if (await _attendanceAdminAccess.IsAttendanceAdminAsync(cancellationToken))
            return;

        // Direct manager: read-only, current Employee.ManagerId only (former managers and archived reports excluded).
        // An id that does not exist is never a direct report, so ids cannot be probed.
        if (linked is not null
            && await _managerTeamAccess.CanViewTeamDashboardAsync(cancellationToken)
            && await _managerTeamAccess.IsDirectReportAsync(linked.Id, employeeId, cancellationToken))
        {
            return;
        }

        throw new BusinessRuleException(AttendanceAccessMessages.Denied);
    }

    public async Task<AttendanceReportScope> ResolveReportScopeAsync(
        int organizationId,
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        // Organisation scoping for attendance admins is out of scope for B07 (B33-style follow-up).
        if (await _attendanceAdminAccess.IsAttendanceAdminAsync(cancellationToken))
            return AttendanceReportScope.Unrestricted;

        var linked = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken)
            ?? throw new BusinessRuleException(AttendanceAccessMessages.Denied);

        if (linked.OrganizationId != organizationId)
            throw new BusinessRuleException(AttendanceAccessMessages.Denied);

        var allowed = new List<int> { linked.Id };
        if (await _managerTeamAccess.CanViewTeamDashboardAsync(cancellationToken))
            allowed.AddRange(await GetDirectReportIdsAsync(linked.Id, cancellationToken));

        if (employeeId.HasValue)
        {
            if (!allowed.Contains(employeeId.Value))
                throw new BusinessRuleException(AttendanceAccessMessages.Denied);

            return AttendanceReportScope.Only([employeeId.Value]);
        }

        return AttendanceReportScope.Only(allowed);
    }

    public Task<bool> CanManageOtherEmployeesAttendanceAsync(CancellationToken cancellationToken = default)
        => _attendanceAdminAccess.IsAttendanceAdminAsync(cancellationToken);

    private async Task<List<int>> GetDirectReportIdsAsync(int managerEmployeeId, CancellationToken cancellationToken)
    {
        // Same definition as IManagerTeamAccessService.IsDirectReportAsync: current ManagerId, not archived.
        return await _employeeRepository.GetQueryable()
            .AsNoTracking()
            .Where(e => e.ManagerId == managerEmployeeId && !e.IsArchived)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }
}
