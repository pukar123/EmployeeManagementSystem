using EMS.Application.DTOs.Shift;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Shifts;

public sealed class ShiftService : IShiftService
{
    private readonly IBaseRepository<Shift> _shiftRepository;
    private readonly IBaseRepository<Employee> _employeeRepository;

    public ShiftService(
        IBaseRepository<Shift> shiftRepository,
        IBaseRepository<Employee> employeeRepository)
    {
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<ShiftResponseModel> CreateAsync(CreateShiftRequestModel request, CancellationToken cancellationToken = default)
    {
        request.Title = StringHelper.NormalizeRequired(request.Title);
        request.Description = StringHelper.NormalizeOptional(request.Description);
        ValidateTimeframe(request.StartAtUtc, request.EndAtUtc);

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null || employee.IsArchived)
            throw new BusinessRuleException("Employee was not found.");

        if (employee.OrganizationId != request.OrganizationId)
            throw new BusinessRuleException("Shift organization must match the employee organization.");

        var now = DateTime.UtcNow;
        var entity = ShiftMapper.ToEntity(request);
        entity.CreatedAtUtc = now;
        entity.UpdatedAtUtc = now;

        await _shiftRepository.AddAsync(entity, cancellationToken);
        await _shiftRepository.SaveChangesAsync(cancellationToken);

        return ShiftMapper.ToResponse(entity);
    }

    public async Task<ShiftResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _shiftRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : ShiftMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<ShiftResponseModel>> GetAllAsync(
        int? organizationId,
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        var query = _shiftRepository.GetQueryable().AsNoTracking();

        if (organizationId.HasValue)
            query = query.Where(s => s.OrganizationId == organizationId.Value);

        if (employeeId.HasValue)
            query = query.Where(s => s.EmployeeId == employeeId.Value);

        var list = await query
            .OrderByDescending(s => s.StartAtUtc)
            .ThenBy(s => s.Title)
            .ToListAsync(cancellationToken);

        return list.Select(ShiftMapper.ToResponse).ToList();
    }

    /// <summary>
    /// Shifts relevant to the employee portal: in-progress or not yet finished (scheduled/started),
    /// so a scheduled block that already began but has not ended still appears.
    /// </summary>
    public async Task<IReadOnlyList<ShiftResponseModel>> GetUpcomingByEmployeeAsync(
        int employeeId,
        DateTime? fromUtc,
        CancellationToken cancellationToken = default)
    {
        var from = fromUtc ?? DateTime.UtcNow;

        var list = await _shiftRepository.GetQueryable()
            .AsNoTracking()
            .Where(s =>
                s.EmployeeId == employeeId
                && s.EndAtUtc >= from
                && (s.Status == ShiftStatus.Scheduled || s.Status == ShiftStatus.Started))
            .OrderByDescending(s => s.Status == ShiftStatus.Started ? 1 : 0)
            .ThenBy(s => s.StartAtUtc)
            .ThenBy(s => s.Title)
            .ToListAsync(cancellationToken);

        return list.Select(ShiftMapper.ToResponse).ToList();
    }

    public async Task<ShiftResponseModel?> UpdateAsync(int id, UpdateShiftRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _shiftRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        request.Title = StringHelper.NormalizeRequired(request.Title);
        request.Description = StringHelper.NormalizeOptional(request.Description);
        ValidateTimeframe(request.StartAtUtc, request.EndAtUtc);

        ShiftMapper.ApplyUpdate(entity, request);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _shiftRepository.Update(entity);
        await _shiftRepository.SaveChangesAsync(cancellationToken);

        return ShiftMapper.ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _shiftRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        _shiftRepository.Remove(entity);
        await _shiftRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ShiftResponseModel?> StartShiftAsync(int shiftId, int employeeId, CancellationToken cancellationToken = default)
    {
        var entity = await _shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        if (entity is null)
            return null;

        if (entity.EmployeeId != employeeId)
            throw new BusinessRuleException("Shift does not belong to the current employee.");

        if (entity.Status != ShiftStatus.Scheduled)
            throw new BusinessRuleException("Only scheduled shifts can be started.");

        var hasStarted = await _shiftRepository.GetQueryable()
            .AnyAsync(
                s => s.EmployeeId == employeeId && s.Status == ShiftStatus.Started && s.Id != shiftId,
                cancellationToken);

        if (hasStarted)
            throw new BusinessRuleException("Complete or cancel your in-progress shift before starting another.");

        var now = DateTime.UtcNow;
        entity.Status = ShiftStatus.Started;
        entity.UpdatedAtUtc = now;

        _shiftRepository.Update(entity);
        await _shiftRepository.SaveChangesAsync(cancellationToken);

        return ShiftMapper.ToResponse(entity);
    }

    private static void ValidateTimeframe(DateTime startAtUtc, DateTime endAtUtc)
    {
        ValidateUtcDate(startAtUtc, "Start time must include a valid UTC-aware timestamp.");
        ValidateUtcDate(endAtUtc, "End time must include a valid UTC-aware timestamp.");

        if (startAtUtc > endAtUtc)
            throw new BusinessRuleException("Shift start must be before or equal to shift end.");
    }

    private static void ValidateUtcDate(DateTime value, string message)
    {
        if (value.Kind == DateTimeKind.Unspecified)
            throw new BusinessRuleException(message);
    }
}
