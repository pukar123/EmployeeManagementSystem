using EMS.Application.DTOs.Organization;
using Pukar.Shared;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Organizations;

public sealed class OrganizationService : IOrganizationService
{
    private readonly IBaseRepository<Organization> _repository;

    public OrganizationService(IBaseRepository<Organization> repository)
    {
        _repository = repository;
    }

    public async Task<OrganizationDTO> CreateAsync(OrganizationDTO dto, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await _repository.GetQueryable().AnyAsync(cancellationToken))
                throw new BusinessRuleException("Only one organization is allowed per instance.");

            dto.Name = StringHelper.NormalizeRequired(dto.Name);
            dto.Code = StringHelper.NormalizeOptional(dto.Code);
            dto.Description = StringHelper.NormalizeOptional(dto.Description);
            dto.Motto = StringHelper.NormalizeOptional(dto.Motto);

            if (await NameAlreadyExistsAsync(dto.Name, cancellationToken))
                throw new BusinessRuleException("An organization with this name already exists.");

            if (!string.IsNullOrWhiteSpace(dto.Code) &&
                await CodeAlreadyExistsAsync(dto.Code, cancellationToken))
                throw new BusinessRuleException("An organization with this code already exists.");

            var entity = new Organization();
            MapDtoToEntity(dto, entity);

            await _repository.AddAsync(entity, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            dto.Id = entity.Id;
            dto.LogoRelativePath = entity.LogoRelativePath;
            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<OrganizationResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : OrganizationMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<OrganizationResponseModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(OrganizationMapper.ToResponse).ToList();
    }

    public async Task<OrganizationResponseModel?> UpdateAsync(int id, UpdateOrganizationRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        request.Name = StringHelper.NormalizeRequired(request.Name);
        request.Code = StringHelper.NormalizeOptional(request.Code);
        request.Description = StringHelper.NormalizeOptional(request.Description);
        request.Motto = StringHelper.NormalizeOptional(request.Motto);

        OrganizationMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return OrganizationMapper.ToResponse(entity);
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

    private async Task<bool> NameAlreadyExistsAsync(string name, CancellationToken cancellationToken)
    {
        var key = name.Trim().ToLowerInvariant();
        return await _repository.GetQueryable()
            .AnyAsync(o => o.Name.Trim().ToLower() == key, cancellationToken);
    }

    private async Task<bool> CodeAlreadyExistsAsync(string code, CancellationToken cancellationToken)
    {
        var key = code.Trim();
        return await _repository.GetQueryable()
            .AnyAsync(o => o.Code != null && o.Code.Trim() == key, cancellationToken);
    }

    public async Task<OrganizationResponseModel?> UpdateLogoRelativePathAsync(
        int id,
        string logoRelativePath,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        entity.LogoRelativePath = logoRelativePath;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return OrganizationMapper.ToResponse(entity);
    }

    private static void MapDtoToEntity(OrganizationDTO dto, Organization entity)
    {
        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.IsActive = dto.IsActive;
        entity.Description = dto.Description;
        entity.Motto = dto.Motto;
    }
}
