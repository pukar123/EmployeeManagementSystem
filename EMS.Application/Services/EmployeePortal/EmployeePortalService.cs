using System.Globalization;
using EMS.Application.DTOs.EmployeePortal;
using EMS.Application.DTOs.Shift;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Leave;
using EMS.Application.Services.Shifts;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.EmployeePortal;

public sealed class EmployeePortalService : IEmployeePortalService
{
    private const int MaxLeaveRequestsInPortal = 50;

    private readonly IIdentityContext _identityContext;
    private readonly IBaseRepository<Employee> _employeeRepository;
    private readonly IShiftService _shiftService;
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly ILeaveBalanceService _leaveBalanceService;

    public EmployeePortalService(
        IIdentityContext identityContext,
        IBaseRepository<Employee> employeeRepository,
        IShiftService shiftService,
        ILeaveRequestService leaveRequestService,
        ILeaveBalanceService leaveBalanceService)
    {
        _identityContext = identityContext;
        _employeeRepository = employeeRepository;
        _shiftService = shiftService;
        _leaveRequestService = leaveRequestService;
        _leaveBalanceService = leaveBalanceService;
    }

    public async Task<EmployeePortalResponseModel> GetPortalAsync(CancellationToken cancellationToken = default)
    {
        var employee = await ResolveLinkedEmployeeAsync(cancellationToken);

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
        var employee = await ResolveLinkedEmployeeAsync(cancellationToken);
        return await _shiftService.StartShiftAsync(shiftId, employee.Id, cancellationToken);
    }

    private async Task<Employee> ResolveLinkedEmployeeAsync(CancellationToken cancellationToken)
    {
        var identity = _identityContext.GetCurrent();
        if (identity.UserId is null)
            throw new BusinessRuleException("User is not authenticated.");

        var externalKey = identity.UserId.Value.ToString(CultureInfo.InvariantCulture);
        var employee = await _employeeRepository.GetQueryable()
            .AsNoTracking()
            .Where(e => !e.IsArchived && e.ExternalIdentityKey == externalKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
            throw new BusinessRuleException("No employee profile is linked to this user account.");

        return employee;
    }
}
