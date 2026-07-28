using EMS.Application.DTOs.EmployeePortal;
using EMS.Application.DTOs.Shift;
using EMS.Application.DTOs.Task;
using EMS.Application.Services.Leave;
using EMS.Application.Services.Shifts;
using EMS.Application.Services.Tasks;
using EMS.Domain.Enums;
using Pukar.Shared;

namespace EMS.Application.Services.EmployeePortal;

public sealed class EmployeePortalService : IEmployeePortalService
{
    private const int MaxLeaveRequestsInPortal = 50;

    private readonly ILinkedEmployeeService _linkedEmployeeService;
    private readonly ILeaveEmployeeAccessService _leaveEmployeeAccessService;
    private readonly IShiftService _shiftService;
    private readonly ITaskService _taskService;
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly ILeaveBalanceService _leaveBalanceService;

    public EmployeePortalService(
        ILinkedEmployeeService linkedEmployeeService,
        ILeaveEmployeeAccessService leaveEmployeeAccessService,
        IShiftService shiftService,
        ITaskService taskService,
        ILeaveRequestService leaveRequestService,
        ILeaveBalanceService leaveBalanceService)
    {
        _linkedEmployeeService = linkedEmployeeService;
        _leaveEmployeeAccessService = leaveEmployeeAccessService;
        _shiftService = shiftService;
        _taskService = taskService;
        _leaveRequestService = leaveRequestService;
        _leaveBalanceService = leaveBalanceService;
    }

    public async Task<EmployeePortalEligibilityResponseModel> GetEligibilityAsync(CancellationToken cancellationToken = default)
    {
        var employee = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        var canManageOthers = await _leaveEmployeeAccessService.CanManageOtherEmployeesLeaveAsync(cancellationToken);

        return new EmployeePortalEligibilityResponseModel
        {
            HasLinkedEmployeeProfile = employee is not null,
            LinkedEmployeeId = employee?.Id,
            LinkedOrganizationId = employee?.OrganizationId,
            CanManageOtherEmployeesLeave = canManageOthers,
        };
    }

    public async Task<EmployeePortalResponseModel> GetPortalAsync(CancellationToken cancellationToken = default)
    {
        var employee = await _linkedEmployeeService.TryGetLinkedEmployeeAsync(cancellationToken);
        if (employee is null)
        {
            return new EmployeePortalResponseModel
            {
                HasLinkedEmployeeProfile = false,
            };
        }

        var fromUtc = DateTime.UtcNow;
        var shifts = (await _shiftService.GetUpcomingByEmployeeAsync(employee.Id, fromUtc, cancellationToken)).ToList();

        var activeTasks = (await _taskService.GetAllAsync(employeeId: employee.Id, cancellationToken: cancellationToken))
            .Where(t => t.Status != TaskWorkflowStatus.Completed)
            .ToList();

        var schedule = BuildMergedSchedule(shifts, activeTasks);

        var leaveRequests = (await _leaveRequestService.GetByEmployeeAsync(employee.Id, cancellationToken))
            .OrderByDescending(r => r.StartDateUtc)
            .Take(MaxLeaveRequestsInPortal)
            .ToList();

        var leaveBalances = (await _leaveBalanceService.GetByEmployeeAsync(employee.Id, cancellationToken)).ToList();

        return new EmployeePortalResponseModel
        {
            HasLinkedEmployeeProfile = true,
            EmployeeId = employee.Id,
            OrganizationId = employee.OrganizationId,
            Schedule = schedule,
            LeaveBalances = leaveBalances,
            LeaveRequests = leaveRequests,
        };
    }

    /// <summary>
    /// Single portal timeline: shifts and open tasks interleaved.
    /// In-progress work (shift Started, task InProgress) sorts first so the hero reflects current work;
    /// then items order by effective start (shift start; task start or assignment time), then title.
    /// </summary>
    private static IReadOnlyList<PortalScheduleEntryResponseModel> BuildMergedSchedule(
        IReadOnlyList<ShiftResponseModel> shifts,
        IReadOnlyList<TaskResponseModel> tasks)
    {
        var entries = new List<PortalScheduleEntryResponseModel>(shifts.Count + tasks.Count);
        foreach (var s in shifts)
        {
            entries.Add(new PortalScheduleEntryResponseModel
            {
                Kind = PortalScheduleEntryKind.Shift,
                Shift = s,
            });
        }

        foreach (var t in tasks)
        {
            entries.Add(new PortalScheduleEntryResponseModel
            {
                Kind = PortalScheduleEntryKind.Task,
                Task = t,
            });
        }

        return entries
            .OrderByDescending(IsLiveWork)
            .ThenBy(EffectiveStartUtc)
            .ThenBy(EntryTitle)
            .ToList();
    }

    private static bool IsLiveWork(PortalScheduleEntryResponseModel e) =>
        e.Kind == PortalScheduleEntryKind.Shift
            ? e.Shift!.Status == ShiftStatus.Started
            : e.Task!.Status == TaskWorkflowStatus.InProgress;

    private static DateTime EffectiveStartUtc(PortalScheduleEntryResponseModel e) =>
        e.Kind == PortalScheduleEntryKind.Shift
            ? e.Shift!.StartAtUtc
            : e.Task!.StartAtUtc ?? e.Task.AssignedAtUtc;

    private static string EntryTitle(PortalScheduleEntryResponseModel e) =>
        e.Kind == PortalScheduleEntryKind.Shift ? e.Shift!.Title : e.Task!.Title;

    public async Task<ShiftResponseModel?> StartShiftAsync(int shiftId, CancellationToken cancellationToken = default)
    {
        var employee = await _linkedEmployeeService.GetLinkedEmployeeOrThrowAsync(cancellationToken);
        return await _shiftService.StartShiftAsync(shiftId, employee.Id, cancellationToken);
    }

    public async Task<TaskResponseModel?> StartTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var employee = await _linkedEmployeeService.GetLinkedEmployeeOrThrowAsync(cancellationToken);
        var task = await _taskService.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
            return null;

        if (task.EmployeeId != employee.Id)
            throw new BusinessRuleException("Task does not belong to the current employee.");

        if (task.Status is not (TaskWorkflowStatus.Assigned or TaskWorkflowStatus.Blocked))
            throw new BusinessRuleException("Only assigned or blocked tasks can be started from the portal.");

        return await _taskService.UpdateStatusAsync(
            taskId,
            new UpdateTaskStatusRequestModel { Status = TaskWorkflowStatus.InProgress },
            cancellationToken);
    }
}
