using EMS.Application.DTOs.Authorization;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Authorization;

public sealed class RoleKeyCapabilityService : IRoleKeyCapabilityService
{
    private const string AdminRoleKey = "ADMIN";

    private readonly IBaseRepository<RoleKeyCapability> _roleKeyCapabilities;

    public RoleKeyCapabilityService(IBaseRepository<RoleKeyCapability> roleKeyCapabilities)
    {
        _roleKeyCapabilities = roleKeyCapabilities;
    }

    public async Task<RoleCapabilityAccessResponseModel> GetAsync(string roleKey, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoleKey(roleKey);
        var keys = await _roleKeyCapabilities.GetQueryable()
            .AsNoTracking()
            .Where(r => r.RoleKey == normalized && r.Allowed)
            .Select(r => r.CapabilityKey)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return new RoleCapabilityAccessResponseModel
        {
            RoleKey = normalized,
            CapabilityKeys = keys,
        };
    }

    public async Task SetAsync(string roleKey, SetRoleCapabilityAccessRequestModel request, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoleKey(roleKey);
        var distinctKeys = request.CapabilityKeys?
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        foreach (var key in distinctKeys)
        {
            if (!EmployeeCapabilities.All.Contains(key, StringComparer.Ordinal))
                throw new BusinessRuleException($"Unknown capability key: {key}");
        }

        if (normalized == AdminRoleKey && distinctKeys.Count == 0)
            throw new BusinessRuleException("Cannot remove all employee capabilities for the ADMIN role.");

        await using var tx = await _roleKeyCapabilities.BeginTransactionAsync(cancellationToken);
        var existing = await _roleKeyCapabilities.GetQueryable()
            .Where(r => r.RoleKey == normalized)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
            _roleKeyCapabilities.RemoveRange(existing);

        foreach (var capabilityKey in distinctKeys)
        {
            await _roleKeyCapabilities.AddAsync(
                new RoleKeyCapability
                {
                    RoleKey = normalized,
                    CapabilityKey = capabilityKey,
                    Allowed = true,
                },
                cancellationToken);
        }

        await _roleKeyCapabilities.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private static string NormalizeRoleKey(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
            throw new BusinessRuleException("Role key is required.");
        return roleKey.Trim().ToUpperInvariant();
    }
}
