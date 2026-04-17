using System.Globalization;
using System.Text.RegularExpressions;
using EMS.Application.DTOs.Employee;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeIdentityProvisioningService : IEmployeeIdentityProvisioningService
{
    private static readonly Regex PasswordSegmentCleaner = new(
        @"[^A-Za-z0-9]",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private readonly IBaseRepository<Employee> _employees;
    private readonly IBaseRepository<Organization> _organizations;
    private readonly IEmployeeUserManagementGateway _gateway;

    public EmployeeIdentityProvisioningService(
        IBaseRepository<Employee> employees,
        IBaseRepository<Organization> organizations,
        IEmployeeUserManagementGateway gateway)
    {
        _employees = employees;
        _organizations = organizations;
        _gateway = gateway;
    }

    public async Task<ProvisionEmployeeUserResponseModel> ProvisionAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
            throw new BusinessRuleException("Employee was not found.");

        var organization = await _organizations.GetByIdAsync(employee.OrganizationId, cancellationToken);
        if (organization is null)
            throw new BusinessRuleException("Organization was not found.");

        var existingUser = await ResolveLinkedUserAsync(employee, cancellationToken);
        var isNewUser = existingUser is null;
        string? temporaryPassword = null;

        if (existingUser is null)
        {
            temporaryPassword = GenerateTemporaryPassword(employee);
            existingUser = await _gateway.CreateUserAsync(
                new CreateEmployeeLinkedUserRequest
                {
                    Email = employee.Email,
                    UserName = BuildEmployeeName(employee),
                    Password = temporaryPassword,
                    IsActive = employee.IsActive,
                    MustChangePassword = true,
                },
                cancellationToken);
        }

        var externalKey = existingUser.Id.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(employee.ExternalIdentityKey, externalKey, StringComparison.Ordinal))
        {
            employee.ExternalIdentityKey = externalKey;
            _employees.Update(employee);
            await _employees.SaveChangesAsync(cancellationToken);
        }

        var assignedRoleIds = await _gateway.GetRoleIdsForUserAsync(existingUser.Id, cancellationToken);
        return new ProvisionEmployeeUserResponseModel
        {
            EmployeeName = BuildEmployeeName(employee),
            EmployeeNumber = employee.EmployeeNumber,
            Email = existingUser.Email,
            TemporaryPassword = temporaryPassword,
            IsNewUser = isNewUser,
            AssignedRoleIds = assignedRoleIds,
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

        var userId = await ResolveLinkedUserIdAsync(employee, cancellationToken);
        await _gateway.SetRoleIdsForUserAsync(userId, request.RoleIds ?? Array.Empty<int>(), cancellationToken);
    }

    private async Task<EmployeeLinkedUserSnapshot?> ResolveLinkedUserAsync(
        Employee employee,
        CancellationToken cancellationToken)
    {
        if (int.TryParse(employee.ExternalIdentityKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var linkedUserId))
        {
            var linkedUser = await _gateway.GetUserByIdAsync(linkedUserId, cancellationToken);
            if (linkedUser is not null)
                return linkedUser;
        }

        return await _gateway.GetUserByEmailAsync(employee.Email, cancellationToken);
    }

    private async Task<int> ResolveLinkedUserIdAsync(Employee employee, CancellationToken cancellationToken)
    {
        var linkedUser = await ResolveLinkedUserAsync(employee, cancellationToken);
        if (linkedUser is null)
            throw new BusinessRuleException("Linked user account has not been provisioned for this employee.");

        var externalKey = linkedUser.Id.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(employee.ExternalIdentityKey, externalKey, StringComparison.Ordinal))
        {
            employee.ExternalIdentityKey = externalKey;
            _employees.Update(employee);
            await _employees.SaveChangesAsync(cancellationToken);
        }

        return linkedUser.Id;
    }

    private static string BuildEmployeeName(Employee employee)
    {
        return $"{employee.FirstName} {employee.LastName}".Trim();
    }

    private static string GenerateTemporaryPassword(Employee employee)
    {
        var firstName = PasswordSegmentCleaner.Replace(employee.FirstName?.Trim() ?? string.Empty, string.Empty);
        if (string.IsNullOrWhiteSpace(firstName))
        {
            var employeeNumber = PasswordSegmentCleaner.Replace(employee.EmployeeNumber.Trim(), string.Empty);
            if (string.IsNullOrWhiteSpace(employeeNumber))
                throw new BusinessRuleException("Employee number is required before creating a linked user.");

            firstName = $"EMP{employeeNumber}";
        }

        return $"{firstName}@123";
    }
}
