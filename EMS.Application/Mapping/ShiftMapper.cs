using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using ShiftDtos = EMS.Application.DTOs.Shift;

namespace EMS.Application.Mapping;

internal static class ShiftMapper
{
    public static Shift ToEntity(ShiftDtos.CreateShiftRequestModel request)
    {
        return new Shift
        {
            OrganizationId = request.OrganizationId,
            EmployeeId = request.EmployeeId,
            SiteId = request.SiteId,
            Title = request.Title,
            Description = request.Description,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            Status = ShiftStatus.Scheduled,
        };
    }

    public static void ApplyUpdate(Shift entity, ShiftDtos.UpdateShiftRequestModel request)
    {
        entity.SiteId = request.SiteId;
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.StartAtUtc = request.StartAtUtc;
        entity.EndAtUtc = request.EndAtUtc;
        entity.Status = request.Status;
    }

    public static ShiftDtos.ShiftResponseModel ToResponse(Shift entity)
    {
        return new ShiftDtos.ShiftResponseModel
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            EmployeeId = entity.EmployeeId,
            SiteId = entity.SiteId,
            Title = entity.Title,
            Description = entity.Description,
            StartAtUtc = entity.StartAtUtc,
            EndAtUtc = entity.EndAtUtc,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }
}
