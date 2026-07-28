using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Pukar.Shared;

namespace EMS.Application.Mapping;

internal static class EmployeeMapper
{
    public static void NormalizeCreateRequest(CreateEmployeeRequestModel request)
    {
        request.FirstName = StringHelper.NormalizeRequired(request.FirstName);
        request.LastName = StringHelper.NormalizeRequired(request.LastName);
        request.PhoneNumber = EmployeeRelationshipValidator.NormalizePhoneNumber(request.PhoneNumber);
    }

    public static void NormalizeUpdateRequest(UpdateEmployeeRequestModel request)
    {
        request.FirstName = StringHelper.NormalizeRequired(request.FirstName);
        request.LastName = StringHelper.NormalizeRequired(request.LastName);
        request.PhoneNumber = EmployeeRelationshipValidator.NormalizePhoneNumber(request.PhoneNumber);
    }

    public static Employee ToEntity(CreateEmployeeRequestModel request)
    {
        return new Employee
        {
            OrganizationId = request.OrganizationId,
            DepartmentId = request.DepartmentId,
            LocationId = request.LocationId,
            ManagerId = request.ManagerId,
            JobPositionId = request.JobPositionId,
            EmployeeNumber = string.Empty,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            DateJoined = request.DateJoined,
            EmploymentStatus = request.EmploymentStatus,
            IsActive = request.EmploymentStatus == EmploymentStatus.Active
        };
    }

    public static bool IsEmploymentActive(EmploymentStatus status) => status == EmploymentStatus.Active;

    public static void ApplyProfileUpdate(Employee entity, UpdateEmployeeRequestModel request)
    {
        entity.OrganizationId = request.OrganizationId;
        entity.LocationId = request.LocationId;
        entity.FirstName = request.FirstName;
        entity.LastName = request.LastName;
        entity.Email = request.Email;
        entity.PhoneNumber = request.PhoneNumber;
        entity.DateOfBirth = request.DateOfBirth;
        entity.DateJoined = request.DateJoined;
    }

    public static EmployeeProfileResponseModel ToProfile(
        Employee entity,
        string? departmentName,
        string? jobPositionTitle,
        string? jobPositionCode,
        string? managerName,
        string? managerEmployeeNumber,
        string? locationLabel,
        IReadOnlyList<string> siteNames,
        string? primarySiteName,
        bool hasLinkedLogin,
        int? linkedUserId,
        string? linkedUserEmail,
        bool? linkedLoginIsActive)
    {
        return new EmployeeProfileResponseModel
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            EmployeeNumber = entity.EmployeeNumber,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            DateOfBirth = entity.DateOfBirth,
            DateJoined = entity.DateJoined,
            EmploymentStatus = entity.EmploymentStatus,
            IsActive = entity.IsActive,
            IsArchived = entity.IsArchived,
            ArchivedAtUtc = entity.ArchivedAtUtc,
            RetentionUntilUtc = entity.RetentionUntilUtc,
            ArchiveReason = entity.ArchiveReason,
            DepartmentId = entity.DepartmentId,
            DepartmentName = departmentName,
            JobPositionId = entity.JobPositionId,
            JobPositionTitle = jobPositionTitle,
            JobPositionCode = jobPositionCode,
            ManagerId = entity.ManagerId,
            ManagerName = managerName,
            ManagerEmployeeNumber = managerEmployeeNumber,
            LocationId = entity.LocationId,
            LocationLabel = locationLabel,
            PrimarySiteName = primarySiteName,
            SiteNames = siteNames,
            HasLinkedLogin = hasLinkedLogin,
            LinkedUserId = linkedUserId,
            LinkedUserEmail = linkedUserEmail,
            LinkedLoginIsActive = linkedLoginIsActive,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }

    public static EmployeeResponseModel ToResponse(Employee entity)
    {
        return new EmployeeResponseModel
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            DepartmentId = entity.DepartmentId,
            LocationId = entity.LocationId,
            ManagerId = entity.ManagerId,
            JobPositionId = entity.JobPositionId,
            EmployeeNumber = entity.EmployeeNumber,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            DateOfBirth = entity.DateOfBirth,
            DateJoined = entity.DateJoined,
            EmploymentStatus = entity.EmploymentStatus,
            IsActive = entity.IsActive,
            IsArchived = entity.IsArchived,
            ArchivedAtUtc = entity.ArchivedAtUtc,
            RetentionUntilUtc = entity.RetentionUntilUtc,
            ArchiveReason = entity.ArchiveReason,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }
}
