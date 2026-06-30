using System.Text.RegularExpressions;
using EMS.Application.DTOs.Employee;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Employees;

public sealed class EmployeeRelationshipValidator
{
    private const int MaxManagerChainDepth = 64;

    private readonly IBaseRepository<Organization> _organizations;
    private readonly IBaseRepository<Department> _departments;
    private readonly IBaseRepository<Location> _locations;
    private readonly IBaseRepository<JobPosition> _jobPositions;
    private readonly IBaseRepository<Employee> _employees;

    public EmployeeRelationshipValidator(
        IBaseRepository<Organization> organizations,
        IBaseRepository<Department> departments,
        IBaseRepository<Location> locations,
        IBaseRepository<JobPosition> jobPositions,
        IBaseRepository<Employee> employees)
    {
        _organizations = organizations;
        _departments = departments;
        _locations = locations;
        _jobPositions = jobPositions;
        _employees = employees;
    }

    public (string Email, string? PhoneNumber) NormalizeAndValidateProfile(
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        DateTime dateOfBirth,
        DateTime dateJoined,
        EmploymentStatus employmentStatus)
    {
        _ = StringHelper.NormalizeRequired(firstName);
        _ = StringHelper.NormalizeRequired(lastName);

        var normalizedEmail = StringHelper.NormalizeRequired(email);
        if (!StringHelper.IsValidEmail(normalizedEmail))
            throw new BusinessRuleException("Invalid email address.");

        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        ValidateDates(dateOfBirth, dateJoined);
        ValidateEmploymentStatus(employmentStatus);

        return (normalizedEmail, normalizedPhone);
    }

    public async Task ValidateRelationshipsForCreateOrUpdateAsync(
        int employeeId,
        int organizationId,
        int? departmentId,
        int? locationId,
        int? jobPositionId,
        int? managerId,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
            throw new BusinessRuleException("Organization was not found.");

        if (departmentId is int deptId)
            await ValidateDepartmentAsync(organizationId, deptId, cancellationToken);

        if (locationId is int locId)
            await ValidateLocationAsync(organizationId, locId, cancellationToken);

        if (jobPositionId is int posId)
            await ValidateJobPositionAsync(organizationId, posId, cancellationToken);

        if (managerId is int mgrId)
            await ValidateManagerAsync(employeeId, organizationId, mgrId, cancellationToken);

        await EnsureEmailUniqueInOrganizationAsync(employeeId, organizationId, normalizedEmail, cancellationToken);
    }

    public void EnsureNotArchived(Employee employee)
    {
        if (employee.IsArchived)
            throw new BusinessRuleException("Archived employees cannot be modified.");
    }

    public void EnsureProfileDoesNotChangeOrganizationalFields(
        Employee entity,
        UpdateEmployeeRequestModel request)
    {
        if (entity.DepartmentId != request.DepartmentId
            || entity.JobPositionId != request.JobPositionId
            || entity.ManagerId != request.ManagerId)
        {
            throw new BusinessRuleException(
                "Use employee transfer operations to change department, position, or manager.");
        }
    }

    public static string? NormalizePhoneNumber(string? phoneNumber)
    {
        var trimmed = StringHelper.NormalizeOptional(phoneNumber);
        if (trimmed is null)
            return null;

        return Regex.Replace(trimmed, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
    }

    private static void ValidateDates(DateTime dateOfBirth, DateTime dateJoined)
    {
        var today = DateTime.UtcNow.Date;
        if (dateOfBirth.Date >= today)
            throw new BusinessRuleException("Date of birth must be in the past.");

        if (dateJoined.Date <= dateOfBirth.Date)
            throw new BusinessRuleException("Date joined must be after date of birth.");
    }

    private static void ValidateEmploymentStatus(EmploymentStatus employmentStatus)
    {
        if (!Enum.IsDefined(employmentStatus))
            throw new BusinessRuleException("Invalid employment status.");
    }

    private async Task ValidateDepartmentAsync(int organizationId, int departmentId, CancellationToken cancellationToken)
    {
        var department = await _departments.GetByIdAsync(departmentId, cancellationToken);
        if (department is null)
            throw new BusinessRuleException("Department was not found.");

        if (department.OrganizationId != organizationId)
            throw new BusinessRuleException("Department must belong to the same organization as the employee.");

        if (!department.IsActive)
            throw new BusinessRuleException("Department is not active.");
    }

    private async Task ValidateLocationAsync(int organizationId, int locationId, CancellationToken cancellationToken)
    {
        var location = await _locations.GetByIdAsync(locationId, cancellationToken);
        if (location is null)
            throw new BusinessRuleException("Location was not found.");

        if (location.OrganizationId != organizationId)
            throw new BusinessRuleException("Location must belong to the same organization as the employee.");

        if (!location.IsActive)
            throw new BusinessRuleException("Location is not active.");
    }

    private async Task ValidateJobPositionAsync(int organizationId, int jobPositionId, CancellationToken cancellationToken)
    {
        var jobPosition = await _jobPositions.GetByIdAsync(jobPositionId, cancellationToken);
        if (jobPosition is null)
            throw new BusinessRuleException("Job position was not found.");

        if (jobPosition.OrganizationId != organizationId)
            throw new BusinessRuleException("Job position must belong to the same organization as the employee.");

        if (!jobPosition.IsActive)
            throw new BusinessRuleException("Job position is not active.");
    }

    private async Task ValidateManagerAsync(
        int employeeId,
        int organizationId,
        int managerId,
        CancellationToken cancellationToken)
    {
        if (managerId == employeeId)
            throw new BusinessRuleException("An employee cannot be their own manager.");

        var manager = await _employees.GetByIdAsync(managerId, cancellationToken);
        if (manager is null)
            throw new BusinessRuleException("Manager was not found.");

        if (manager.OrganizationId != organizationId)
            throw new BusinessRuleException("Manager must belong to the same organization as the employee.");

        if (manager.IsArchived)
            throw new BusinessRuleException("Manager is archived.");

        if (!manager.IsActive || manager.EmploymentStatus != EmploymentStatus.Active)
            throw new BusinessRuleException("Manager must be an active employee.");

        await EnsureNoManagerCycleAsync(employeeId, managerId, cancellationToken);
    }

    private async Task EnsureNoManagerCycleAsync(int employeeId, int managerId, CancellationToken cancellationToken)
    {
        if (employeeId == 0)
            return;

        var visited = new HashSet<int> { employeeId };
        var currentId = managerId;

        for (var depth = 0; depth < MaxManagerChainDepth && currentId != 0; depth++)
        {
            if (!visited.Add(currentId))
                throw new BusinessRuleException("Manager assignment would create a circular reporting hierarchy.");

            var current = await _employees.GetQueryable()
                .AsNoTracking()
                .Where(e => e.Id == currentId)
                .Select(e => new { e.ManagerId })
                .FirstOrDefaultAsync(cancellationToken);

            if (current is null)
                break;

            currentId = current.ManagerId ?? 0;
        }
    }

    private async Task EnsureEmailUniqueInOrganizationAsync(
        int employeeId,
        int organizationId,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var emailUpper = EmailNormalizer.Normalize(normalizedEmail);
        var duplicate = await _employees.GetQueryable()
            .AnyAsync(
                e => e.OrganizationId == organizationId
                    && e.Id != employeeId
                    && e.Email.ToUpper() == emailUpper,
                cancellationToken);

        if (duplicate)
            throw new BusinessRuleException("An employee with this email already exists in the organization.");
    }
}
