using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeRoleService : IEmployeeRoleService
{
    private const string SourcePositionInherited = "position_inherited";
    private const string SourceDirectOverride = "direct_override";

    private readonly IBaseRepository<Employee> _employeeRepository;
    private readonly IBaseRepository<EmployeeRoleAssignment> _employeeRoleAssignmentRepository;
    private readonly IBaseRepository<JobPosition> _jobPositionRepository;
    private readonly IUserManagementRoleMetadataClient _roleMetadataClient;
    private readonly IEmployeeRoleSyncService _employeeRoleSyncService;
    private readonly ILegacyRoleKeyGuard _legacyRoleKeyGuard;

    public EmployeeRoleService(
        IBaseRepository<Employee> employeeRepository,
        IBaseRepository<EmployeeRoleAssignment> employeeRoleAssignmentRepository,
        IBaseRepository<JobPosition> jobPositionRepository,
        IUserManagementRoleMetadataClient roleMetadataClient,
        IEmployeeRoleSyncService employeeRoleSyncService,
        ILegacyRoleKeyGuard legacyRoleKeyGuard)
    {
        _employeeRepository = employeeRepository;
        _employeeRoleAssignmentRepository = employeeRoleAssignmentRepository;
        _jobPositionRepository = jobPositionRepository;
        _roleMetadataClient = roleMetadataClient;
        _employeeRoleSyncService = employeeRoleSyncService;
        _legacyRoleKeyGuard = legacyRoleKeyGuard;
    }

    public async Task<IReadOnlyList<EmployeeEffectiveRoleResponseModel>?> GetEffectiveRolesAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null || employee.IsArchived)
            return null;

        var assignments = await _employeeRoleAssignmentRepository.GetQueryable()
            .Where(x => x.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var roleMetadataByKey = (await _roleMetadataClient.GetRolesAsync(cancellationToken))
            .ToDictionary(x => x.NormalizedName, StringComparer.OrdinalIgnoreCase);

        var jobPositionIds = assignments
            .Where(x => x.Source == EmployeeRoleSource.PositionInherited && x.JobPositionId.HasValue)
            .Select(x => x.JobPositionId!.Value)
            .Distinct()
            .ToList();

        var positionTitleById = await _jobPositionRepository.GetQueryable()
            .Where(x => jobPositionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        var response = assignments
            .Select(x =>
            {
                var metadataExists = roleMetadataByKey.TryGetValue(x.RoleKey, out var metadata);
                var source = x.Source == EmployeeRoleSource.PositionInherited
                    ? SourcePositionInherited
                    : SourceDirectOverride;

                return new EmployeeEffectiveRoleResponseModel
                {
                    RoleKey = x.RoleKey,
                    RoleName = metadataExists ? metadata!.Name : x.RoleKey,
                    RoleNormalizedName = metadataExists ? metadata!.NormalizedName : x.RoleKey,
                    Source = source,
                    JobPositionId = x.Source == EmployeeRoleSource.PositionInherited ? x.JobPositionId : null,
                    JobPositionTitle = x.Source == EmployeeRoleSource.PositionInherited && x.JobPositionId is int jobPositionId
                        ? positionTitleById.GetValueOrDefault(jobPositionId)
                        : null,
                    IsSystem = metadataExists && metadata!.IsSystem,
                };
            })
            .OrderBy(x => x.RoleName)
            .ThenBy(x => x.Source)
            .ToList();

        return response;
    }

    public async Task<bool> SetDirectRolesAsync(
        int employeeId,
        SetEmployeeDirectRolesRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null || employee.IsArchived)
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
            var metadataByKey = roleMetadata.ToDictionary(x => x.NormalizedName, StringComparer.OrdinalIgnoreCase);
            var invalidRoleKeys = desiredRoleKeys.Where(roleKey => !metadataByKey.ContainsKey(roleKey)).ToList();
            if (invalidRoleKeys.Count > 0)
                throw new BusinessRuleException("One or more selected roles are invalid.");
        }

        var existingDirectAssignments = await _employeeRoleAssignmentRepository.GetQueryable()
            .Where(x => x.EmployeeId == employeeId && x.Source == EmployeeRoleSource.DirectOverride)
            .ToListAsync(cancellationToken);

        var hasChanges = false;
        foreach (var assignment in existingDirectAssignments)
        {
            if (desiredRoleKeys.Contains(assignment.RoleKey.Trim().ToUpperInvariant()))
                continue;

            _employeeRoleAssignmentRepository.Remove(assignment);
            hasChanges = true;
        }

        var existingRoleKeys = existingDirectAssignments
            .Select(x => x.RoleKey.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var roleKey in desiredRoleKeys)
        {
            if (existingRoleKeys.Contains(roleKey))
                continue;

            await _employeeRoleAssignmentRepository.AddAsync(
                new EmployeeRoleAssignment
                {
                    EmployeeId = employeeId,
                    RoleKey = roleKey,
                    Source = EmployeeRoleSource.DirectOverride,
                    JobPositionId = null,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                },
                cancellationToken);
            hasChanges = true;
        }

        if (hasChanges)
            await _employeeRoleAssignmentRepository.SaveChangesAsync(cancellationToken);

        await _employeeRoleSyncService.SyncEmployeeAsync(employeeId, cancellationToken);
        return true;
    }
}
