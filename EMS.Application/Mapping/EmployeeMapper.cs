using EMS.Application.DTOs.Employee;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;

namespace EMS.Application.Mapping;

internal static class EmployeeMapper
{
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

    public static void ApplyUpdate(Employee entity, UpdateEmployeeRequestModel request)
    {
        entity.OrganizationId = request.OrganizationId;
        entity.DepartmentId = request.DepartmentId;
        entity.LocationId = request.LocationId;
        entity.ManagerId = request.ManagerId;
        entity.JobPositionId = request.JobPositionId;
        entity.FirstName = request.FirstName;
        entity.LastName = request.LastName;
        entity.Email = request.Email;
        entity.PhoneNumber = request.PhoneNumber;
        entity.DateOfBirth = request.DateOfBirth;
        entity.DateJoined = request.DateJoined;
        entity.EmploymentStatus = request.EmploymentStatus;
        entity.IsActive = request.EmploymentStatus == EmploymentStatus.Active;
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
