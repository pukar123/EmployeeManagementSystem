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
    private readonly ILegacyRoleKeyGuard _legacyRoleKeyGuard;

    public PositionRoleService(
        IBaseRepository<JobPosition> jobPositionRepository,
        IBaseRepository<PositionRole> positionRoleRepository,
        IBaseRepository<Employee> employeeRepository,
        IBaseRepository<EmployeeRoleAssignment> employeeRoleAssignmentRepository,
        IUserManagementRoleMetadataClient roleMetadataClient,
        IEmployeeRoleSyncService employeeRoleSyncService,
        ILegacyRoleKeyGuard legacyRoleKeyGuard)
    {
        _jobPositionRepository = jobPositionRepository;
        _positionRoleRepository = positionRoleRepository;
        _employeeRepository = employeeRepository;
        _employeeRoleAssignmentRepository = employeeRoleAssignmentRepository;
        _roleMetadataClient = roleMetadataClient;
        _employeeRoleSyncService = employeeRoleSyncService;
        _legacyRoleKeyGuard = legacyRoleKeyGuard;
    }

    public async Task<IReadOnlyList<PositionRoleResponseModel>?> GetByPositionAsync(
        int jobPositionId,
        CancellationToken cancellationToken = default)
    {
        var position = await _jobPositionRepository.GetByIdAsync(jobPositionId, cancellationToken);
        if (position is null)
            return null;

        var roleKeys = await _positionRoleRepository.GetQueryable()
            .Where(x => x.JobPositionId == jobPositionId)
            .Select(x => x.RoleKey)
            .Distinct()
            .ToListAsync(cancellationToken);

        var roleMetadataByKey = (await _roleMetadataClient.GetRolesAsync(cancellationToken))
            .ToDictionary(x => x.NormalizedName, StringComparer.OrdinalIgnoreCase);

        return roleKeys
            .Select(roleKey =>
            {
                var found = roleMetadataByKey.TryGetValue(roleKey, out var metadata);
                return new PositionRoleResponseModel
                {
                    RoleKey = roleKey,
                    RoleName = found ? metadata!.Name : roleKey,
                    RoleNormalizedName = found ? metadata!.NormalizedName : roleKey,
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

        await _legacyRoleKeyGuard.EnsureRoleMutationsAllowedAsync(cancellationToken);

        var desiredRoleKeys = (request.RoleKeys ?? Array.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var roleMetadata = await _roleMetadataClient.GetRolesAsync(cancellationToken);
        if (roleMetadata.Count > 0)
        {
            var roleMetadataByKey = roleMetadata.ToDictionary(x => x.NormalizedName, StringComparer.OrdinalIgnoreCase);
            var invalidRoleKeys = desiredRoleKeys.Where(roleKey => !roleMetadataByKey.ContainsKey(roleKey)).ToList();
            if (invalidRoleKeys.Count > 0)
                throw new BusinessRuleException("One or more selected roles are invalid.");
        }

        await EnsureEmployeesStillHaveAtLeastOneRoleAsync(jobPositionId, desiredRoleKeys.Count, cancellationToken);

        var existingMappings = await _positionRoleRepository.GetQueryable()
            .Where(x => x.JobPositionId == jobPositionId)
            .ToListAsync(cancellationToken);

        var existingRoleKeys = existingMappings
            .Select(x => x.RoleKey.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);
        var hasChanges = false;

        foreach (var mapping in existingMappings)
        {
            if (desiredRoleKeys.Contains(mapping.RoleKey.Trim().ToUpperInvariant()))
                continue;

            _positionRoleRepository.Remove(mapping);
            hasChanges = true;
        }

        foreach (var roleKey in desiredRoleKeys)
        {
            if (existingRoleKeys.Contains(roleKey))
                continue;

            await _positionRoleRepository.AddAsync(
                new PositionRole
                {
                    JobPositionId = jobPositionId,
                    RoleKey = roleKey,
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
            .Select(x => new { EmployeeId = x.Key, Count = x.Select(i => i.RoleKey).Distinct().Count() })
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
