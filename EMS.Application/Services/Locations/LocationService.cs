using EMS.Application.DTOs.Location;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;

namespace EMS.Application.Services.Locations;

public sealed class LocationService : ILocationService
{
    private readonly IBaseRepository<Location> _repository;

    public LocationService(IBaseRepository<Location> repository)
    {
        _repository = repository;
    }

    public async Task<LocationResponseModel> CreateAsync(CreateLocationRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = LocationMapper.ToEntity(request);
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return LocationMapper.ToResponse(entity);
    }

    public async Task<LocationResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : LocationMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<LocationResponseModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(LocationMapper.ToResponse).ToList();
    }

    public async Task<LocationResponseModel?> UpdateAsync(int id, UpdateLocationRequestModel request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        LocationMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return LocationMapper.ToResponse(entity);
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
}
