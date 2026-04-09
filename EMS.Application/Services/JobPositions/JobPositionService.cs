using EMS.Application.DTOs.JobPosition;
using Pukar.Shared;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using JobPositionEntity = EMS.Domain.DbModels.JobPosition;

namespace EMS.Application.Services.JobPositions;

public sealed class JobPositionService : IJobPositionService
{
    private readonly IBaseRepository<JobPositionEntity> _repository;
    private readonly IBaseRepository<Employee> _employeeRepository;

    public JobPositionService(
        IBaseRepository<JobPositionEntity> repository,
        IBaseRepository<Employee> employeeRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
    }

    public async Task<JobPositionResponseModel> CreateAsync(CreateJobPositionRequestModel request, CancellationToken cancellationToken = default)
    {
        request.Title = StringHelper.NormalizeRequired(request.Title);
        request.Code = StringHelper.NormalizeOptional(request.Code);
        request.Description = StringHelper.NormalizeOptional(request.Description);

        if (await TitleExistsInOrgAsync(request.OrganizationId, request.Title, cancellationToken))
            throw new BusinessRuleException("A job position with this title already exists in the organization.");

        if (!string.IsNullOrWhiteSpace(request.Code) &&
            await CodeExistsInOrgAsync(request.OrganizationId, request.Code, cancellationToken))
            throw new BusinessRuleException("A job position with this code already exists in the organization.");

        var entity = JobPositionMapper.ToEntity(request);
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return JobPositionMapper.ToResponse(entity);
    }

    public async Task<JobPositionResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : JobPositionMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<JobPositionResponseModel>> GetByOrganizationAsync(int organizationId, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetQueryable()
            .Where(j => j.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
        return list.Select(JobPositionMapper.ToResponse).ToList();
    }

    public async Task<JobPositionResponseModel?> UpdateAsync(int id, UpdateJobPositionRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        request.Title = StringHelper.NormalizeRequired(request.Title);
        request.Code = StringHelper.NormalizeOptional(request.Code);
        request.Description = StringHelper.NormalizeOptional(request.Description);

        if (await TitleExistsInOrgAsync(entity.OrganizationId, request.Title, cancellationToken, id))
            throw new BusinessRuleException("A job position with this title already exists in the organization.");

        if (!string.IsNullOrWhiteSpace(request.Code) &&
            await CodeExistsInOrgAsync(entity.OrganizationId, request.Code, cancellationToken, id))
            throw new BusinessRuleException("A job position with this code already exists in the organization.");

        JobPositionMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return JobPositionMapper.ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return false;

        if (await _employeeRepository.GetQueryable().AnyAsync(e => e.JobPositionId == id, cancellationToken))
            throw new BusinessRuleException("Cannot delete a job position that is assigned to employees. Unassign employees first.");

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> TitleExistsInOrgAsync(int organizationId, string title, CancellationToken cancellationToken, int? exceptId = null)
    {
        var key = title.Trim().ToLowerInvariant();
        var q = _repository.GetQueryable().Where(
            j => j.OrganizationId == organizationId && j.Title.Trim().ToLower() == key);
        if (exceptId is int eid)
            q = q.Where(j => j.Id != eid);
        return await q.AnyAsync(cancellationToken);
    }

    private async Task<bool> CodeExistsInOrgAsync(int organizationId, string code, CancellationToken cancellationToken, int? exceptId = null)
    {
        var key = code.Trim();
        var q = _repository.GetQueryable().Where(
            j => j.OrganizationId == organizationId && j.Code != null && j.Code.Trim() == key);
        if (exceptId is int eid)
            q = q.Where(j => j.Id != eid);
        return await q.AnyAsync(cancellationToken);
    }
}
