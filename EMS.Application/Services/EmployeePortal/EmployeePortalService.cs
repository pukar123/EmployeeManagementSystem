using EMS.Application.DTOs.EmployeePortal;
using EMS.Application.DTOs.Shift;
using EMS.Application.Services.Leave;
using EMS.Application.Services.Shifts;
using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.EmployeePortal;

public sealed class EmployeePortalService : IEmployeePortalService
{
    private const int MaxLeaveRequestsInPortal = 50;

    private readonly ILinkedEmployeeService _linkedEmployeeService;
    private readonly ILeaveEmployeeAccessService _leaveEmployeeAccessService;
    private readonly IShiftService _shiftService;
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly ILeaveBalanceService _leaveBalanceService;

    public EmployeePortalService(
        ILinkedEmployeeService linkedEmployeeService,
        ILeaveEmployeeAccessService leaveEmployeeAccessService,
        IShiftService shiftService,
        ILeaveRequestService leaveRequestService,
        ILeaveBalanceService leaveBalanceService)
    {
        _linkedEmployeeService = linkedEmployeeService;
        _leaveEmployeeAccessService = leaveEmployeeAccessService;
        _shiftService = shiftService;
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
        var upcoming = (await _shiftService.GetUpcomingByEmployeeAsync(employee.Id, fromUtc, cancellationToken)).ToList();

        var nearest = upcoming.Count > 0 ? upcoming[0] : null;
        var topThree = upcoming.Take(3).ToList();

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
            NearestUpcomingShift = nearest,
            TopThreeUpcomingShifts = topThree,
            AllUpcomingShifts = upcoming,
            LeaveBalances = leaveBalances,
            LeaveRequests = leaveRequests,
        };
    }

    public async Task<ShiftResponseModel?> StartShiftAsync(int shiftId, CancellationToken cancellationToken = default)
    {
        var employee = await _linkedEmployeeService.GetLinkedEmployeeOrThrowAsync(cancellationToken);
        return await _shiftService.StartShiftAsync(shiftId, employee.Id, cancellationToken);
    }
}
