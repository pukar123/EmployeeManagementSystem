using System.Globalization;
using EMS.Application.DTOs.Employee;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeIdentityProvisioningService : IEmployeeIdentityProvisioningService
{
    private readonly IBaseRepository<Employee> _employees;
    private readonly IEmployeeUserManagementGateway _gateway;
    private readonly EmployeeRelationshipValidator _validator;

    public EmployeeIdentityProvisioningService(
        IBaseRepository<Employee> employees,
        IEmployeeUserManagementGateway gateway,
        EmployeeRelationshipValidator validator)
    {
        _employees = employees;
        _gateway = gateway;
        _validator = validator;
    }

    public async Task<ProvisionEmployeeUserResponseModel> LinkExistingUserAsync(
        int employeeId,
        LinkEmployeeUserRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
            throw new BusinessRuleException("Employee was not found.");

        _validator.EnsureNotArchived(employee);

        var user = await _gateway.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            throw new BusinessRuleException("User account was not found.");

        if (!EmailNormalizer.Normalize(user.Email).Equals(
                EmailNormalizer.Normalize(employee.Email),
                StringComparison.Ordinal))
        {
            throw new BusinessRuleException("User account email must match the employee email before linking.");
        }

        await LinkEmployeeToUserAsync(employee, user, cancellationToken);

        var assignedRoleKeys = await _gateway.GetRoleKeysForUserAsync(user.Id, cancellationToken);
        return new ProvisionEmployeeUserResponseModel
        {
            EmployeeName = BuildEmployeeName(employee),
            EmployeeNumber = employee.EmployeeNumber,
            Email = user.Email,
            IsNewUser = false,
            AssignedRoleKeys = assignedRoleKeys,
        };
    }

    public async Task AssignRolesAsync(
        int employeeId,
        AssignEmployeeUserRolesRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
            throw new BusinessRuleException("Employee was not found.");

        _validator.EnsureNotArchived(employee);

        var linkedUser = await EmployeeLinkedIdentityHelper.TryResolveLinkedUserAsync(employee, _gateway, cancellationToken);
        if (linkedUser is null)
            throw new BusinessRuleException("Linked user account has not been provisioned for this employee.");

        var roleKeys = (request.RoleKeys ?? Array.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        await _gateway.SetRoleKeysForUserAsync(
            linkedUser.Id,
            roleKeys,
            $"assign-roles:employee:{employeeId}",
            cancellationToken);
    }

    public async Task ReactivateLoginAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
            throw new BusinessRuleException("Employee was not found.");

        _validator.EnsureNotArchived(employee);

        if (employee.EmploymentStatus != EmploymentStatus.Active)
            throw new BusinessRuleException("Login can only be reactivated for active employees.");

        var linkedUser = await EmployeeLinkedIdentityHelper.TryResolveLinkedUserAsync(employee, _gateway, cancellationToken);
        if (linkedUser is null)
            throw new BusinessRuleException("Linked user account has not been provisioned for this employee.");

        await _gateway.ActivateLinkedUserAsync(
            linkedUser.Id,
            $"reactivate:employee:{employeeId}",
            cancellationToken);
    }

    private async Task LinkEmployeeToUserAsync(
        Employee employee,
        EmployeeLinkedUserSnapshot user,
        CancellationToken cancellationToken)
    {
        var externalKey = user.Id.ToString(CultureInfo.InvariantCulture);
        await EmployeeLinkedIdentityHelper.EnsureNoOtherActiveEmployeeUsesExternalIdentityKeyAsync(
            _employees,
            employee.Id,
            externalKey,
            cancellationToken);

        employee.ExternalIdentityKey = externalKey;
        _employees.Update(employee);
        await _employees.SaveChangesAsync(cancellationToken);
    }

    private static string BuildEmployeeName(Employee employee)
        => $"{employee.FirstName} {employee.LastName}".Trim();
}
