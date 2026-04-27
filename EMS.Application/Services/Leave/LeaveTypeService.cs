using EMS.Application.DTOs.Leave;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveTypeService : ILeaveTypeService
{
    private readonly IBaseRepository<LeaveType> _leaveTypeRepository;

    public LeaveTypeService(IBaseRepository<LeaveType> leaveTypeRepository)
    {
        _leaveTypeRepository = leaveTypeRepository;
    }

    public async Task<IReadOnlyList<LeaveTypeResponseModel>> GetByOrganizationAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _leaveTypeRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(LeaveMapper.ToResponse).ToList();
    }

    public async Task<LeaveTypeResponseModel> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _leaveTypeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Leave type was not found.");

        return LeaveMapper.ToResponse(entity);
    }

    public async Task<LeaveTypeResponseModel> CreateAsync(
        CreateLeaveTypeRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Leave type name is required.");

        var exists = await _leaveTypeRepository.GetQueryable()
            .AnyAsync(
                x => x.OrganizationId == request.OrganizationId && x.Name == name,
                cancellationToken);
        if (exists)
            throw new BusinessRuleException("A leave type with the same name already exists.");

        var entity = new LeaveType
        {
            OrganizationId = request.OrganizationId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Unit = request.Unit,
            RequiresAttachment = request.RequiresAttachment,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        await _leaveTypeRepository.AddAsync(entity, cancellationToken);
        await _leaveTypeRepository.SaveChangesAsync(cancellationToken);
        return LeaveMapper.ToResponse(entity);
    }

    public async Task<LeaveTypeResponseModel> UpdateAsync(
        int id,
        UpdateLeaveTypeRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _leaveTypeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Leave type was not found.");

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Leave type name is required.");

        var duplicate = await _leaveTypeRepository.GetQueryable()
            .AnyAsync(
                x => x.Id != id && x.OrganizationId == entity.OrganizationId && x.Name == name,
                cancellationToken);
        if (duplicate)
            throw new BusinessRuleException("A leave type with the same name already exists.");

        entity.Name = name;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.Unit = request.Unit;
        entity.RequiresAttachment = request.RequiresAttachment;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _leaveTypeRepository.Update(entity);
        await _leaveTypeRepository.SaveChangesAsync(cancellationToken);
        return LeaveMapper.ToResponse(entity);
    }
}
