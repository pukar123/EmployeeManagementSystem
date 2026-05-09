using EMS.Application.DTOs.JobPosition;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.JobPositions;

public sealed class PositionRoleService : IPositionRoleService
{
    private readonly IBaseRepository<JobPosition> _jobPositionRepository;
    private readonly IBaseRepository<PositionRole> _positionRoleRepository;
    private readonly IBaseRepository<Employee> _employeeRepository;
    private readonly IBaseRepository<EmployeeRoleAssignment> _employeeRoleAssignmentRepository;
    private readonly IUserManagementRoleMetadataClient _roleMetadataClient;
    private readonly IEmployeeRoleSyncService _employeeRoleSyncService;

    public PositionRoleService(
        IBaseRepository<JobPosition> jobPositionRepository,
        IBaseRepository<PositionRole> positionRoleRepository,
        IBaseRepository<Employee> employeeRepository,
        IBaseRepository<EmployeeRoleAssignment> employeeRoleAssignmentRepository,
        IUserManagementRoleMetadataClient roleMetadataClient,
        IEmployeeRoleSyncService employeeRoleSyncService)
    {
        _jobPositionRepository = jobPositionRepository;
        _positionRoleRepository = positionRoleRepository;
        _employeeRepository = employeeRepository;
        _employeeRoleAssignmentRepository = employeeRoleAssignmentRepository;
        _roleMetadataClient = roleMetadataClient;
        _employeeRoleSyncService = employeeRoleSyncService;
    }

    public async Task<IReadOnlyList<PositionRoleResponseModel>?> GetByPositionAsync(
        int jobPositionId,
        CancellationToken cancellationToken = default)
    {
        var position = await _jobPositionRepository.GetByIdAsync(jobPositionId, cancellationToken);
        if (position is null)
            return null;

        var roleIds = await _positionRoleRepository.GetQueryable()
            .Where(x => x.JobPositionId == jobPositionId)
            .Select(x => x.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var roleMetadataById = (await _roleMetadataClient.GetRolesAsync(cancellationToken))
            .ToDictionary(x => x.Id);

        return roleIds
            .Select(roleId =>
            {
                var found = roleMetadataById.TryGetValue(roleId, out var metadata);
                return new PositionRoleResponseModel
                {
                    RoleId = roleId,
                    RoleName = found ? metadata!.Name : $"Role #{roleId}",
                    RoleNormalizedName = found ? metadata!.NormalizedName : string.Empty,
                    IsSystem = found && metadata!.IsSystem,
                };
            })
            .OrderBy(x => x.RoleName)
            .ToList();
    }

    public async Task<bool> SetPositionRolesAsync(
        int jobPositionId,
        SetPositionRolesRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var position = await _jobPositionRepository.GetByIdAsync(jobPositionId, cancellationToken);
        if (position is null)
            return false;

        var desiredRoleIds = (request.RoleIds ?? Array.Empty<int>())
            .Distinct()
            .ToHashSet();

        var roleMetadata = await _roleMetadataClient.GetRolesAsync(cancellationToken);
        if (roleMetadata.Count > 0)
        {
            var roleMetadataById = roleMetadata.ToDictionary(x => x.Id);
            var invalidRoleIds = desiredRoleIds.Where(roleId => !roleMetadataById.ContainsKey(roleId)).ToList();
            if (invalidRoleIds.Count > 0)
                throw new BusinessRuleException("One or more selected roles are invalid.");
        }

        await EnsureEmployeesStillHaveAtLeastOneRoleAsync(jobPositionId, desiredRoleIds.Count, cancellationToken);

        var existingMappings = await _positionRoleRepository.GetQueryable()
            .Where(x => x.JobPositionId == jobPositionId)
            .ToListAsync(cancellationToken);

        var existingRoleIds = existingMappings.Select(x => x.RoleId).ToHashSet();
        var hasChanges = false;

        foreach (var mapping in existingMappings)
        {
            if (desiredRoleIds.Contains(mapping.RoleId))
                continue;

            _positionRoleRepository.Remove(mapping);
            hasChanges = true;
        }

        foreach (var roleId in desiredRoleIds)
        {
            if (existingRoleIds.Contains(roleId))
                continue;

            await _positionRoleRepository.AddAsync(
                new PositionRole
                {
                    JobPositionId = jobPositionId,
                    RoleId = roleId,
                    CreatedAtUtc = DateTime.UtcNow,
                },
                cancellationToken);
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _positionRoleRepository.SaveChangesAsync(cancellationToken);
            await _employeeRoleSyncService.SyncEmployeesForPositionAsync(jobPositionId, cancellationToken);
        }

        return true;
    }

    private async Task EnsureEmployeesStillHaveAtLeastOneRoleAsync(
        int jobPositionId,
        int inheritedRoleCountAfterChange,
        CancellationToken cancellationToken)
    {
        var employeeIds = await _employeeRepository.GetQueryable()
            .Where(x => x.JobPositionId == jobPositionId && !x.IsArchived)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (employeeIds.Count == 0)
            return;

        var directRoleCounts = await _employeeRoleAssignmentRepository.GetQueryable()
            .Where(x =>
                employeeIds.Contains(x.EmployeeId)
                && x.Source == EmployeeRoleSource.DirectOverride)
            .GroupBy(x => x.EmployeeId)
            .Select(x => new { EmployeeId = x.Key, Count = x.Select(i => i.RoleId).Distinct().Count() })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Count, cancellationToken);

        foreach (var employeeId in employeeIds)
        {
            var directCount = directRoleCounts.GetValueOrDefault(employeeId);
            var effectiveCountAfterChange = directCount + inheritedRoleCountAfterChange;
            if (effectiveCountAfterChange > 0)
                continue;

            throw new BusinessRuleException(
                "Cannot remove roles from this position because at least one assigned employee would have zero effective roles.");
        }
    }
}
