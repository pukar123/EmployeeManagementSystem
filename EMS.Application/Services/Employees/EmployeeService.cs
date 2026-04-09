using System.Globalization;
using System.Text.RegularExpressions;
using EMS.Application.DTOs.Employee;
using Pukar.Shared;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using JobPositionEntity = EMS.Domain.DbModels.JobPosition;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeService : IEmployeeService
{
    private static readonly Regex EmployeeNumberSequence = new(
        @"^EMP(\d+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private readonly IBaseRepository<Employee> _repository;
    private readonly IBaseRepository<JobPositionEntity> _jobPositionRepository;

    public EmployeeService(IBaseRepository<Employee> repository, IBaseRepository<JobPositionEntity> jobPositionRepository)
    {
        _repository = repository;
        _jobPositionRepository = jobPositionRepository;
    }

    public async Task<EmployeeResponseModel> CreateAsync(CreateEmployeeRequestModel request, CancellationToken cancellationToken = default)
    {
        request.Email = StringHelper.NormalizeRequired(request.Email);
        if (!StringHelper.IsValidEmail(request.Email))
            throw new BusinessRuleException("Invalid email address.");

        await EnsureJobPositionMatchesOrganizationAsync(request.OrganizationId, request.JobPositionId, cancellationToken);

        var now = DateTime.UtcNow;
        var entity = EmployeeMapper.ToEntity(request);
        entity.EmployeeNumber = await AllocateNextEmployeeNumberAsync(request.OrganizationId, cancellationToken);
        entity.CreatedAtUtc = now;
        entity.UpdatedAtUtc = now;

        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return EmployeeMapper.ToResponse(entity);
    }

    public async Task<EmployeeResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : EmployeeMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<EmployeeResponseModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(EmployeeMapper.ToResponse).ToList();
    }

    public async Task<EmployeeResponseModel?> UpdateAsync(int id, UpdateEmployeeRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        request.Email = StringHelper.NormalizeRequired(request.Email);
        if (!StringHelper.IsValidEmail(request.Email))
            throw new BusinessRuleException("Invalid email address.");

        await EnsureJobPositionMatchesOrganizationAsync(request.OrganizationId, request.JobPositionId, cancellationToken);

        EmployeeMapper.ApplyUpdate(entity, request);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return EmployeeMapper.ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureJobPositionMatchesOrganizationAsync(int organizationId, int? jobPositionId, CancellationToken cancellationToken)
    {
        if (jobPositionId is null)
            return;

        var jobPosition = await _jobPositionRepository.GetQueryable()
            .FirstOrDefaultAsync(j => j.Id == jobPositionId.Value, cancellationToken);

        if (jobPosition is null)
            throw new BusinessRuleException("Job position was not found.");

        if (jobPosition.OrganizationId != organizationId)
            throw new BusinessRuleException("Job position must belong to the same organization as the employee.");
    }

    /// <summary>
    /// Next available code in the form EMP001, EMP002, … per organization (based on existing EMP+digits numbers).
    /// </summary>
    private async Task<string> AllocateNextEmployeeNumberAsync(int organizationId, CancellationToken cancellationToken)
    {
        var rawNumbers = await _repository.GetQueryable()
            .Where(e => e.OrganizationId == organizationId)
            .Select(e => e.EmployeeNumber)
            .ToListAsync(cancellationToken);

        var max = 0;
        foreach (var raw in rawNumbers)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var m = EmployeeNumberSequence.Match(raw.Trim());
            if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                max = Math.Max(max, n);
        }

        return $"EMP{(max + 1).ToString("D3", CultureInfo.InvariantCulture)}";
    }
}
