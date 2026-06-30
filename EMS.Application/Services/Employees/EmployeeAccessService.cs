using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Authorization;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeAccessService : IEmployeeAccessService
{
    private static readonly HashSet<string> AdminRoleKeys = new(StringComparer.Ordinal)
    {
        "ADMIN",
        "ADMINISTRATOR",
    };

    private readonly IIdentityContext _identityContext;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public EmployeeAccessService(
        IIdentityContext identityContext,
        IPermissionEvaluator permissionEvaluator)
    {
        _identityContext = identityContext;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task EnsureCanViewEmployeesAsync(CancellationToken cancellationToken = default)
    {
        if (await CanViewEmployeesAsync(cancellationToken))
            return;

        throw new BusinessRuleException(EmployeeAccessMessages.Denied);
    }

    public async Task EnsureCanManageEmployeesAsync(CancellationToken cancellationToken = default)
    {
        if (await CanManageEmployeesAsync(cancellationToken))
            return;

        throw new BusinessRuleException(EmployeeAccessMessages.Denied);
    }

    public async Task EnsureCanAccessEmployeesAsync(CancellationToken cancellationToken = default)
    {
        if (await CanAccessEmployeesAsync(cancellationToken))
            return;

        throw new BusinessRuleException(EmployeeAccessMessages.Denied);
    }

    public async Task EnsureCanExportEmployeesAsync(CancellationToken cancellationToken = default)
    {
        if (await CanExportEmployeesAsync(cancellationToken))
            return;

        throw new BusinessRuleException(EmployeeAccessMessages.Denied);
    }

    public async Task<EmployeeAccessCapabilitiesResponseModel> GetMyCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return new EmployeeAccessCapabilitiesResponseModel
        {
            View = await CanViewEmployeesAsync(cancellationToken),
            Manage = await CanManageEmployeesAsync(cancellationToken),
            Access = await CanAccessEmployeesAsync(cancellationToken),
            Export = await CanExportEmployeesAsync(cancellationToken),
        };
    }

    private async Task<bool> CanViewEmployeesAsync(CancellationToken cancellationToken)
    {
        if (IsAdmin())
            return true;

        return await HasCapabilityAsync(EmployeeCapabilities.View, cancellationToken);
    }

    private async Task<bool> CanManageEmployeesAsync(CancellationToken cancellationToken)
    {
        if (IsAdmin())
            return true;

        return await HasCapabilityAsync(EmployeeCapabilities.Manage, cancellationToken);
    }

    private async Task<bool> CanAccessEmployeesAsync(CancellationToken cancellationToken)
    {
        if (IsAdmin())
            return true;

        return await HasCapabilityAsync(EmployeeCapabilities.Access, cancellationToken);
    }

    private async Task<bool> CanExportEmployeesAsync(CancellationToken cancellationToken)
    {
        if (IsAdmin())
            return true;

        return await HasCapabilityAsync(EmployeeCapabilities.Export, cancellationToken);
    }

    private bool IsAdmin()
    {
        var actor = _identityContext.GetCurrent();
        return actor.RoleKeys.Any(role => AdminRoleKeys.Contains(role));
    }

    private async Task<bool> HasCapabilityAsync(string capabilityKey, CancellationToken cancellationToken)
    {
        var actor = _identityContext.GetCurrent();
        return await _permissionEvaluator.HasCapabilityAsync(
            actor.RoleKeys.ToList(),
            capabilityKey,
            cancellationToken);
    }
}
