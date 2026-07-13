using EMS.Application.DTOs.Employee;
using EMS.Application.DTOs.Manager;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Manager;

public sealed class ManagerTeamService : IManagerTeamService
{
    private static readonly HashSet<string> AdminRoleKeys = new(StringComparer.Ordinal)
    {
        "ADMIN",
        "ADMINISTRATOR",
    };

    private readonly IManagerTeamAccessService _access;
    private readonly IEmployeeDirectoryService _directoryService;
    private readonly IIdentityContext _identityContext;
    private readonly IBaseRepository<Employee> _employees;
    private readonly IBaseRepository<LeaveRequest> _leaveRequests;
    private readonly IBaseRepository<AttendanceRecord> _attendanceRecords;
    private readonly IBaseRepository<TaskItem> _tasks;
    private readonly IBaseRepository<EmployeeScheduledChange> _scheduledChanges;

    public ManagerTeamService(
        IManagerTeamAccessService access,
        IEmployeeDirectoryService directoryService,
        IIdentityContext identityContext,
        IBaseRepository<Employee> employees,
        IBaseRepository<LeaveRequest> leaveRequests,
        IBaseRepository<AttendanceRecord> attendanceRecords,
        IBaseRepository<TaskItem> tasks,
        IBaseRepository<EmployeeScheduledChange> scheduledChanges)
    {
        _access = access;
        _directoryService = directoryService;
        _identityContext = identityContext;
        _employees = employees;
        _leaveRequests = leaveRequests;
        _attendanceRecords = attendanceRecords;
        _tasks = tasks;
        _scheduledChanges = scheduledChanges;
    }

    public async Task<ManagerTeamDashboardResponseModel> GetDashboardAsync(
        ManagerTeamQueryModel query,
        CancellationToken cancellationToken = default)
    {
        if (query.OrganizationId <= 0)
            throw new BusinessRuleException("Organization id must be greater than zero.");

        var effectiveManagerId = await _access.ResolveEffectiveManagerIdAsync(
            query.OrganizationId,
            query.ManagerId,
            cancellationToken);

        var manager = await _employees.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == effectiveManagerId && e.OrganizationId == query.OrganizationId,
                cancellationToken)
            ?? throw new BusinessRuleException("Manager was not found.");

        var teamIds = await GetTeamMemberIdsAsync(query.OrganizationId, effectiveManagerId, cancellationToken);

        var directoryQuery = new EmployeeDirectoryQueryModel
        {
            OrganizationId = query.OrganizationId,
            ManagerId = effectiveManagerId,
            Page = query.Page,
            PageSize = query.PageSize,
            Search = query.Search,
            EmploymentStatus = query.EmploymentStatus,
            DepartmentId = query.DepartmentId,
            SortBy = query.SortBy,
            SortDirection = query.SortDirection,
            IsArchived = false,
        };

        var directory = await _directoryService.QueryAsync(directoryQuery, cancellationToken);
        var todayUtc = DateTime.UtcNow.Date;

        var summary = await BuildSummaryAsync(query.OrganizationId, teamIds, todayUtc, cancellationToken);
        var pageIds = directory.Items.Select(x => x.Id).ToList();
        var memberExtras = await BuildMemberExtrasAsync(pageIds, todayUtc, cancellationToken);

        var items = directory.Items
            .Select(item => MapMember(item, memberExtras.GetValueOrDefault(item.Id)))
            .ToList();

        return new ManagerTeamDashboardResponseModel
        {
            ManagerId = effectiveManagerId,
            ManagerName = $"{manager.FirstName} {manager.LastName}".Trim(),
            ManagerEmployeeNumber = manager.EmployeeNumber,
            AllowsManagerSelection = IsAdmin(),
            Summary = summary,
            Items = items,
            TotalCount = directory.TotalCount,
            Page = directory.Page,
            PageSize = directory.PageSize,
        };
    }

    private async Task<List<int>> GetTeamMemberIdsAsync(
        int organizationId,
        int managerId,
        CancellationToken cancellationToken)
    {
        return await _employees.GetQueryable()
            .AsNoTracking()
            .Where(e => e.OrganizationId == organizationId && !e.IsArchived && e.ManagerId == managerId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<ManagerTeamSummaryResponseModel> BuildSummaryAsync(
        int organizationId,
        IReadOnlyList<int> teamIds,
        DateTime todayUtc,
        CancellationToken cancellationToken)
    {
        if (teamIds.Count == 0)
        {
            return new ManagerTeamSummaryResponseModel
            {
                AttendanceToday = new ManagerTeamAttendanceTodaySummaryModel { WorkDateUtc = todayUtc },
            };
        }

        var activeCount = await _employees.GetQueryable()
            .AsNoTracking()
            .CountAsync(
                e => teamIds.Contains(e.Id)
                     && e.EmploymentStatus == EmploymentStatus.Active
                     && e.IsActive
                     && !e.IsArchived,
                cancellationToken);

        var pendingLeaveCount = await _leaveRequests.GetQueryable()
            .AsNoTracking()
            .CountAsync(
                x => x.OrganizationId == organizationId
                     && teamIds.Contains(x.EmployeeId)
                     && (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.ModifiedPending),
                cancellationToken);

        var overdueTaskCount = await _tasks.GetQueryable()
            .AsNoTracking()
            .CountAsync(
                t => teamIds.Contains(t.EmployeeId)
                     && t.Status != TaskWorkflowStatus.Completed
                     && t.DueAtUtc.HasValue
                     && t.DueAtUtc.Value < DateTime.UtcNow,
                cancellationToken);

        var upcomingChangeCount = await _scheduledChanges.GetQueryable()
            .AsNoTracking()
            .CountAsync(
                c => teamIds.Contains(c.EmployeeId)
                     && c.Status == EmployeeScheduledChangeStatus.Pending
                     && c.EffectiveAtUtc > DateTime.UtcNow,
                cancellationToken);

        var onLeaveIds = await GetOnLeaveEmployeeIdsAsync(organizationId, teamIds, todayUtc, cancellationToken);
        var attendanceToday = await _attendanceRecords.GetQueryable()
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId
                        && teamIds.Contains(a.EmployeeId)
                        && a.WorkDate == todayUtc)
            .Select(a => new { a.EmployeeId, a.Status })
            .ToListAsync(cancellationToken);

        var presentIds = attendanceToday.Select(a => a.EmployeeId).Distinct().ToHashSet();
        var checkedInOpenCount = attendanceToday.Count(a => a.Status == AttendanceStatus.Open);
        var onLeaveCount = onLeaveIds.Count;

        var activeTeamIds = await _employees.GetQueryable()
            .AsNoTracking()
            .Where(e => teamIds.Contains(e.Id)
                        && e.EmploymentStatus == EmploymentStatus.Active
                        && e.IsActive
                        && !e.IsArchived)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var absentCount = activeTeamIds.Count(id => !presentIds.Contains(id) && !onLeaveIds.Contains(id));
        var presentCount = presentIds.Count;

        return new ManagerTeamSummaryResponseModel
        {
            ActiveEmployeeCount = activeCount,
            PendingLeaveRequestCount = pendingLeaveCount,
            OverdueTaskCount = overdueTaskCount,
            UpcomingScheduledChangeCount = upcomingChangeCount,
            AttendanceToday = new ManagerTeamAttendanceTodaySummaryModel
            {
                WorkDateUtc = todayUtc,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                OnLeaveCount = onLeaveCount,
                CheckedInOpenCount = checkedInOpenCount,
            },
        };
    }

    private async Task<Dictionary<int, MemberExtras>> BuildMemberExtrasAsync(
        IReadOnlyList<int> employeeIds,
        DateTime todayUtc,
        CancellationToken cancellationToken)
    {
        var result = employeeIds.ToDictionary(id => id, _ => new MemberExtras());

        if (employeeIds.Count == 0)
            return result;

        var pendingLeave = await _leaveRequests.GetQueryable()
            .AsNoTracking()
            .Where(x => employeeIds.Contains(x.EmployeeId)
                        && (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.ModifiedPending))
            .GroupBy(x => x.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var row in pendingLeave)
            result[row.EmployeeId].PendingLeaveCount = row.Count;

        var overdueTasks = await _tasks.GetQueryable()
            .AsNoTracking()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                        && t.Status != TaskWorkflowStatus.Completed
                        && t.DueAtUtc.HasValue
                        && t.DueAtUtc.Value < DateTime.UtcNow)
            .GroupBy(t => t.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var row in overdueTasks)
            result[row.EmployeeId].OverdueTaskCount = row.Count;

        var upcomingChanges = await _scheduledChanges.GetQueryable()
            .AsNoTracking()
            .Where(c => employeeIds.Contains(c.EmployeeId)
                        && c.Status == EmployeeScheduledChangeStatus.Pending
                        && c.EffectiveAtUtc > DateTime.UtcNow)
            .GroupBy(c => c.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var row in upcomingChanges)
            result[row.EmployeeId].UpcomingScheduledChangeCount = row.Count;

        var organizationId = await _employees.GetQueryable()
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => e.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        var onLeaveIds = await GetOnLeaveEmployeeIdsAsync(organizationId, employeeIds, todayUtc, cancellationToken);

        var attendanceToday = await _attendanceRecords.GetQueryable()
            .AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.WorkDate == todayUtc)
            .Select(a => new { a.EmployeeId, a.Status })
            .ToListAsync(cancellationToken);

        var attendanceByEmployee = attendanceToday
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var employeeId in employeeIds)
        {
            if (onLeaveIds.Contains(employeeId))
            {
                result[employeeId].TodayAttendanceStatus = ManagerTeamTodayAttendanceStatus.OnLeave;
                continue;
            }

            if (attendanceByEmployee.TryGetValue(employeeId, out var records))
            {
                if (records.Any(r => r.Status == AttendanceStatus.Open))
                    result[employeeId].TodayAttendanceStatus = ManagerTeamTodayAttendanceStatus.CheckedInOpen;
                else
                    result[employeeId].TodayAttendanceStatus = ManagerTeamTodayAttendanceStatus.Present;
            }
            else
            {
                result[employeeId].TodayAttendanceStatus = ManagerTeamTodayAttendanceStatus.Absent;
            }
        }

        return result;
    }

    private async Task<HashSet<int>> GetOnLeaveEmployeeIdsAsync(
        int organizationId,
        IReadOnlyList<int> employeeIds,
        DateTime todayUtc,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
            return [];

        var ids = await _leaveRequests.GetQueryable()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId
                        && employeeIds.Contains(x.EmployeeId)
                        && (x.Status == LeaveRequestStatus.Pending
                            || x.Status == LeaveRequestStatus.ModifiedPending
                            || x.Status == LeaveRequestStatus.Approved)
                        && x.StartDateUtc <= todayUtc
                        && x.EndDateUtc >= todayUtc)
            .Select(x => x.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    private static ManagerTeamMemberResponseModel MapMember(
        EmployeeDirectoryItemResponseModel item,
        MemberExtras? extras)
    {
        extras ??= new MemberExtras();

        return new ManagerTeamMemberResponseModel
        {
            Id = item.Id,
            EmployeeNumber = item.EmployeeNumber,
            FirstName = item.FirstName,
            LastName = item.LastName,
            Email = item.Email,
            EmploymentStatus = item.EmploymentStatus,
            IsActive = item.IsActive,
            IsArchived = item.IsArchived,
            DepartmentId = item.DepartmentId,
            DepartmentName = item.DepartmentName,
            JobPositionId = item.JobPositionId,
            JobPositionTitle = item.JobPositionTitle,
            PrimarySiteName = item.PrimarySiteName,
            PendingLeaveCount = extras.PendingLeaveCount,
            OverdueTaskCount = extras.OverdueTaskCount,
            UpcomingScheduledChangeCount = extras.UpcomingScheduledChangeCount,
            TodayAttendanceStatus = extras.TodayAttendanceStatus,
        };
    }

    private bool IsAdmin()
    {
        var actor = _identityContext.GetCurrent();
        return actor.RoleKeys.Any(role => AdminRoleKeys.Contains(role));
    }

    private sealed class MemberExtras
    {
        public int PendingLeaveCount { get; set; }
        public int OverdueTaskCount { get; set; }
        public int UpcomingScheduledChangeCount { get; set; }
        public ManagerTeamTodayAttendanceStatus TodayAttendanceStatus { get; set; } =
            ManagerTeamTodayAttendanceStatus.Absent;
    }
}
