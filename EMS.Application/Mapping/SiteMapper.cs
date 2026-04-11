using EMS.Application.DTOs.Site;
using EMS.Domain.DbModels;

namespace EMS.Application.Mapping;

internal static class SiteMapper
{
    public static Site ToEntity(CreateSiteRequestModel request)
    {
        return new Site
        {
            SiteName = request.SiteName,
            SiteDescription = request.SiteDescription,
            SiteLocation = request.SiteLocation,
            IsActive = request.IsActive,
            IsDeleted = false
        };
    }

    public static void ApplyUpdate(Site entity, UpdateSiteRequestModel request)
    {
        entity.SiteName = request.SiteName;
        entity.SiteDescription = request.SiteDescription;
        entity.SiteLocation = request.SiteLocation;
        entity.IsActive = request.IsActive;
    }

    public static SiteResponseModel ToResponse(Site entity)
    {
        return new SiteResponseModel
        {
            SiteId = entity.SiteId,
            SiteName = entity.SiteName,
            SiteDescription = entity.SiteDescription,
            SiteLocation = entity.SiteLocation,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted
        };
    }
}
