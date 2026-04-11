using EMS.Application.DTOs.Site;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;

namespace EMS.Application.Services.Sites;

public sealed class SiteService : ISiteService
{
    private readonly IBaseRepository<Site> _repository;
    private readonly IBaseRepository<EmployeeSite> _employeeSiteRepository;

    public SiteService(IBaseRepository<Site> repository, IBaseRepository<EmployeeSite> employeeSiteRepository)
    {
        _repository = repository;
        _employeeSiteRepository = employeeSiteRepository;
    }

    public async Task<SiteResponseModel> CreateAsync(CreateSiteRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = SiteMapper.ToEntity(request);
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return SiteMapper.ToResponse(entity);
    }

    public async Task<SiteResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.IsDeleted)
            return null;
        return SiteMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<SiteResponseModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.FindAsync(s => !s.IsDeleted, cancellationToken);
        return list.Select(SiteMapper.ToResponse).ToList();
    }

    public async Task<SiteResponseModel?> UpdateAsync(int id, UpdateSiteRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.IsDeleted)
            return null;

        SiteMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return SiteMapper.ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.IsDeleted)
            return false;

        var links = (await _employeeSiteRepository.FindAsync(es => es.SiteId == id, cancellationToken)).ToList();
        if (links.Count > 0)
            _employeeSiteRepository.RemoveRange(links);

        entity.IsDeleted = true;
        entity.IsActive = false;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
