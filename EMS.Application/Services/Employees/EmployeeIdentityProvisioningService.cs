using System.Globalization;
using System.Security.Cryptography;
using EMS.Application.DTOs.Employee;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeIdentityProvisioningService : IEmployeeIdentityProvisioningService
{
    private const int TemporaryPasswordLength = 16;

    private readonly IBaseRepository<Employee> _employees;
    private readonly IBaseRepository<Organization> _organizations;
    private readonly IEmployeeUserManagementGateway _gateway;
    private readonly EmployeeRelationshipValidator _validator;

    public EmployeeIdentityProvisioningService(
        IBaseRepository<Employee> employees,
        IBaseRepository<Organization> organizations,
        IEmployeeUserManagementGateway gateway,
        EmployeeRelationshipValidator validator)
    {
        _employees = employees;
        _organizations = organizations;
        _gateway = gateway;
        _validator = validator;
    }

    public async Task<ProvisionEmployeeUserResponseModel> ProvisionAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
            throw new BusinessRuleException("Employee was not found.");

        _validator.EnsureNotArchived(employee);

        var organization = await _organizations.GetByIdAsync(employee.OrganizationId, cancellationToken);
        if (organization is null)
            throw new BusinessRuleException("Organization was not found.");

        if (!string.IsNullOrWhiteSpace(employee.ExternalIdentityKey))
        {
            var linkedUser = await EmployeeLinkedIdentityHelper.TryResolveLinkedUserAsync(employee, _gateway, cancellationToken);
            if (linkedUser is not null)
            {
                var assignedRoleIds = await _gateway.GetRoleIdsForUserAsync(linkedUser.Id, cancellationToken);
                return new ProvisionEmployeeUserResponseModel
                {
                    EmployeeName = BuildEmployeeName(employee),
                    EmployeeNumber = employee.EmployeeNumber,
                    Email = linkedUser.Email,
                    TemporaryPassword = null,
                    IsNewUser = false,
                    AssignedRoleIds = assignedRoleIds,
                };
            }
        }

        var existingByEmail = await _gateway.GetUserByEmailAsync(employee.Email, cancellationToken);
        if (existingByEmail is not null)
        {
            throw new BusinessRuleException(
                "A user account already exists for this email. Use the explicit link-user operation to connect it to this employee.");
        }

        var temporaryPassword = GenerateSecureTemporaryPassword();
        var createdUser = await _gateway.CreateUserAsync(
            new CreateEmployeeLinkedUserRequest
            {
                Email = employee.Email,
                UserName = BuildEmployeeName(employee),
                Password = temporaryPassword,
                IsActive = employee.IsActive,
                MustChangePassword = true,
            },
            cancellationToken);

        await LinkEmployeeToUserAsync(employee, createdUser, cancellationToken);

        var roleIds = await _gateway.GetRoleIdsForUserAsync(createdUser.Id, cancellationToken);
        return new ProvisionEmployeeUserResponseModel
        {
            EmployeeName = BuildEmployeeName(employee),
            EmployeeNumber = employee.EmployeeNumber,
            Email = createdUser.Email,
            TemporaryPassword = temporaryPassword,
            IsNewUser = true,
            AssignedRoleIds = roleIds,
        };
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

        var assignedRoleIds = await _gateway.GetRoleIdsForUserAsync(user.Id, cancellationToken);
        return new ProvisionEmployeeUserResponseModel
        {
            EmployeeName = BuildEmployeeName(employee),
            EmployeeNumber = employee.EmployeeNumber,
            Email = user.Email,
            TemporaryPassword = null,
            IsNewUser = false,
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

        _validator.EnsureNotArchived(employee);

        var linkedUser = await EmployeeLinkedIdentityHelper.TryResolveLinkedUserAsync(employee, _gateway, cancellationToken);
        if (linkedUser is null)
            throw new BusinessRuleException("Linked user account has not been provisioned for this employee.");

        await _gateway.SetRoleIdsForUserAsync(linkedUser.Id, request.RoleIds ?? Array.Empty<int>(), cancellationToken);
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

    private static string GenerateSecureTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        Span<char> chars = stackalloc char[TemporaryPasswordLength];
        Span<byte> bytes = stackalloc byte[TemporaryPasswordLength];

        RandomNumberGenerator.Fill(bytes);
        for (var i = 0; i < TemporaryPasswordLength; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];

        return new string(chars);
    }
}
