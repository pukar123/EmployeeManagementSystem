using EMS.Application.DTOs.Onboarding;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;

namespace EMS.Application.Mapping;

internal static class OnboardingChecklistMapper
{
    public static OnboardingChecklistTemplateItemResponseModel ToItemResponse(OnboardingChecklistTemplateItem entity)
    {
        return new OnboardingChecklistTemplateItemResponseModel
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Category = entity.Category,
            SortOrder = entity.SortOrder,
            DefaultDueDaysFromStart = entity.DefaultDueDaysFromStart,
            DefaultPriority = entity.DefaultPriority,
            IsRequired = entity.IsRequired,
        };
    }

    public static OnboardingChecklistTemplateResponseModel ToResponse(OnboardingChecklistTemplate entity)
    {
        return new OnboardingChecklistTemplateResponseModel
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            IsDefault = entity.IsDefault,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Items = entity.Items
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .Select(ToItemResponse)
                .ToList(),
        };
    }

    public static OnboardingChecklistTemplateItem ToItemEntity(
        OnboardingChecklistTemplateItemRequestModel request,
        int templateId)
    {
        return new OnboardingChecklistTemplateItem
        {
            TemplateId = templateId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            SortOrder = request.SortOrder,
            DefaultDueDaysFromStart = request.DefaultDueDaysFromStart,
            DefaultPriority = request.DefaultPriority,
            IsRequired = request.IsRequired,
        };
    }

    public static void ApplyItemUpdate(
        OnboardingChecklistTemplateItem entity,
        OnboardingChecklistTemplateItemRequestModel request)
    {
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Category = request.Category;
        entity.SortOrder = request.SortOrder;
        entity.DefaultDueDaysFromStart = request.DefaultDueDaysFromStart;
        entity.DefaultPriority = request.DefaultPriority;
        entity.IsRequired = request.IsRequired;
    }
}
