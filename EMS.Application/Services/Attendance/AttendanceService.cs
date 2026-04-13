using EMS.Application.DTOs.Attendance;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Attendance;

public sealed class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAttendanceBreakRepository _breakRepository;
    private readonly IBaseRepository<Employee> _employeeRepository;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IAttendanceBreakRepository breakRepository,
        IBaseRepository<Employee> employeeRepository)
    {
        _attendanceRepository = attendanceRepository;
        _breakRepository = breakRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<AttendanceRecordResponseModel> CheckInAsync(CheckInRequestModel request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("Employee was not found.");
        var at = request.CheckInAtUtc ?? DateTime.UtcNow;

        var open = await _attendanceRepository.GetOpenForEmployeeAsync(request.EmployeeId, cancellationToken);
        if (open is not null)
            throw new BusinessRuleException("Employee already has an active attendance session.");

        var entity = new AttendanceRecord
        {
            OrganizationId = employee.OrganizationId,
            EmployeeId = employee.Id,
            WorkDate = at.Date,
            CheckInAtUtc = at,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            Source = AttendanceSource.Web,
            Status = AttendanceStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        await _attendanceRepository.AddAsync(entity, cancellationToken);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);
        return AttendanceMapper.ToResponse(entity);
    }

    public async Task<AttendanceRecordResponseModel> CheckOutAsync(CheckOutRequestModel request, CancellationToken cancellationToken = default)
    {
        var at = request.CheckOutAtUtc ?? DateTime.UtcNow;
        var open = await _attendanceRepository.GetOpenForEmployeeAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("No active attendance session found.");

        var openBreak = await _breakRepository.GetOpenForAttendanceAsync(open.Id, cancellationToken);
        if (openBreak is not null)
            throw new BusinessRuleException("End the active break before check-out.");

        if (at < open.CheckInAtUtc)
            throw new BusinessRuleException("Check-out time cannot be earlier than check-in time.");

        open.CheckOutAtUtc = at;
        open.CheckOutLatitude = request.Latitude;
        open.CheckOutLongitude = request.Longitude;
        open.Status = AttendanceStatus.Completed;
        open.UpdatedAtUtc = DateTime.UtcNow;

        _attendanceRepository.Update(open);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);
        await LoadBreaksAsync(open, cancellationToken);
        return AttendanceMapper.ToResponse(open);
    }

    public async Task<AttendanceBreakResponseModel> StartBreakAsync(StartBreakRequestModel request, CancellationToken cancellationToken = default)
    {
        var at = request.StartAtUtc ?? DateTime.UtcNow;
        var open = await _attendanceRepository.GetOpenForEmployeeAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("No active attendance session found.");

        var currentBreak = await _breakRepository.GetOpenForAttendanceAsync(open.Id, cancellationToken);
        if (currentBreak is not null)
            throw new BusinessRuleException("An active break already exists.");

        if (at < open.CheckInAtUtc)
            throw new BusinessRuleException("Break start cannot be earlier than check-in.");

        var entity = new AttendanceBreak
        {
            AttendanceRecordId = open.Id,
            StartAtUtc = at,
        };

        await _breakRepository.AddAsync(entity, cancellationToken);
        await _breakRepository.SaveChangesAsync(cancellationToken);
        return AttendanceMapper.ToResponse(entity);
    }

    public async Task<AttendanceBreakResponseModel> EndBreakAsync(EndBreakRequestModel request, CancellationToken cancellationToken = default)
    {
        var at = request.EndAtUtc ?? DateTime.UtcNow;
        var open = await _attendanceRepository.GetOpenForEmployeeAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("No active attendance session found.");

        var currentBreak = await _breakRepository.GetOpenForAttendanceAsync(open.Id, cancellationToken)
            ?? throw new BusinessRuleException("No active break found.");

        if (at < currentBreak.StartAtUtc)
            throw new BusinessRuleException("Break end cannot be earlier than break start.");

        currentBreak.EndAtUtc = at;
        currentBreak.DurationMinutes = Math.Max(0, (int)Math.Round((at - currentBreak.StartAtUtc).TotalMinutes));
        _breakRepository.Update(currentBreak);
        await _breakRepository.SaveChangesAsync(cancellationToken);
        return AttendanceMapper.ToResponse(currentBreak);
    }

    public async Task<AttendanceRecordResponseModel> CreateManualEntryAsync(ManualAttendanceEntryRequestModel request, CancellationToken cancellationToken = default)
    {
        if (request.CheckOutAtUtc <= request.CheckInAtUtc)
            throw new BusinessRuleException("Check-out must be later than check-in.");

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("Employee was not found.");

        if (employee.OrganizationId != request.OrganizationId)
            throw new BusinessRuleException("Employee must belong to the same organization.");

        var entity = new AttendanceRecord
        {
            OrganizationId = request.OrganizationId,
            EmployeeId = request.EmployeeId,
            WorkDate = request.WorkDate.Date,
            CheckInAtUtc = request.CheckInAtUtc,
            CheckOutAtUtc = request.CheckOutAtUtc,
            Source = AttendanceSource.Manual,
            Status = AttendanceStatus.AutoApproved,
            ManualReason = StringHelper.NormalizeOptional(request.ManualReason),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        await _attendanceRepository.AddAsync(entity, cancellationToken);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);
        return AttendanceMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<AttendanceRecordResponseModel>> GetEmployeeAttendanceAsync(
        int employeeId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = _attendanceRepository.GetQueryable()
            .Include(x => x.Breaks)
            .Where(x => x.EmployeeId == employeeId);

        if (fromDate.HasValue)
            query = query.Where(x => x.WorkDate >= fromDate.Value.Date);
        if (toDate.HasValue)
            query = query.Where(x => x.WorkDate <= toDate.Value.Date);

        var rows = await query
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.CheckInAtUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(AttendanceMapper.ToResponse).ToList();
    }

    public async Task<AttendanceSummaryResponseModel> GetSummaryAsync(
        int employeeId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var rows = await _attendanceRepository.GetQueryable()
            .Include(x => x.Breaks)
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.WorkDate >= fromDate.Date &&
                x.WorkDate <= toDate.Date)
            .ToListAsync(cancellationToken);

        var mapped = rows.Select(AttendanceMapper.ToResponse).ToList();

        return new AttendanceSummaryResponseModel
        {
            EmployeeId = employeeId,
            FromDate = fromDate.Date,
            ToDate = toDate.Date,
            TotalRecords = mapped.Count,
            TotalBreakMinutes = mapped.Sum(x => x.BreakMinutes),
            TotalWorkedMinutes = mapped.Sum(x => x.WorkedMinutes),
        };
    }

    private async Task LoadBreaksAsync(AttendanceRecord record, CancellationToken cancellationToken)
    {
        record.Breaks = await _breakRepository.GetQueryable()
            .Where(x => x.AttendanceRecordId == record.Id)
            .OrderBy(x => x.StartAtUtc)
            .ToListAsync(cancellationToken);
    }
}
