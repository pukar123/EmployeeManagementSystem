using EMS.Application.DTOs.Onboarding;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;

namespace EMS.Application.Services.Onboarding;

public sealed class OnboardingChecklistTemplateService : IOnboardingChecklistTemplateService
{
    private readonly IBaseRepository<OnboardingChecklistTemplate> _templateRepository;
    private readonly IBaseRepository<OnboardingChecklistTemplateItem> _itemRepository;
    private readonly IBaseRepository<EmployeeOnboardingChecklist> _checklistRepository;

    public OnboardingChecklistTemplateService(
        IBaseRepository<OnboardingChecklistTemplate> templateRepository,
        IBaseRepository<OnboardingChecklistTemplateItem> itemRepository,
        IBaseRepository<EmployeeOnboardingChecklist> checklistRepository)
    {
        _templateRepository = templateRepository;
        _itemRepository = itemRepository;
        _checklistRepository = checklistRepository;
    }

    public async Task<IReadOnlyList<OnboardingChecklistTemplateResponseModel>> GetByOrganizationAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _templateRepository.GetQueryable()
            .AsNoTracking()
            .Include(t => t.Items)
            .Where(t => t.OrganizationId == organizationId)
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(OnboardingChecklistMapper.ToResponse).ToList();
    }

    public async Task<OnboardingChecklistTemplateResponseModel> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadTemplateWithItemsAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Onboarding checklist template was not found.");

        return OnboardingChecklistMapper.ToResponse(entity);
    }

    public async Task<OnboardingChecklistTemplateResponseModel> CreateAsync(
        CreateOnboardingChecklistTemplateRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var name = StringHelper.NormalizeRequired(request.Name);
        ValidateItems(request.Items);

        await EnsureUniqueNameAsync(request.OrganizationId, name, excludeId: null, cancellationToken);

        var now = DateTime.UtcNow;
        var entity = new OnboardingChecklistTemplate
        {
            OrganizationId = request.OrganizationId,
            Name = name,
            Description = StringHelper.NormalizeOptional(request.Description),
            IsActive = request.IsActive,
            IsDefault = request.IsDefault,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = request.Items
                .OrderBy(i => i.SortOrder)
                .Select(i => OnboardingChecklistMapper.ToItemEntity(i, templateId: 0))
                .ToList(),
        };

        if (entity.IsDefault)
            await ClearDefaultFlagAsync(request.OrganizationId, excludeId: null, cancellationToken);

        await _templateRepository.AddAsync(entity, cancellationToken);
        await _templateRepository.SaveChangesAsync(cancellationToken);

        return OnboardingChecklistMapper.ToResponse(entity);
    }

    public async Task<OnboardingChecklistTemplateResponseModel> UpdateAsync(
        int id,
        UpdateOnboardingChecklistTemplateRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var entity = await LoadTemplateWithItemsAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Onboarding checklist template was not found.");

        var name = StringHelper.NormalizeRequired(request.Name);
        ValidateItems(request.Items);
        await EnsureUniqueNameAsync(entity.OrganizationId, name, excludeId: id, cancellationToken);

        entity.Name = name;
        entity.Description = StringHelper.NormalizeOptional(request.Description);
        entity.IsActive = request.IsActive;
        entity.IsDefault = request.IsDefault;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.IsDefault)
            await ClearDefaultFlagAsync(entity.OrganizationId, excludeId: id, cancellationToken);

        await SyncItemsAsync(entity, request.Items, cancellationToken);

        _templateRepository.Update(entity);
        await _templateRepository.SaveChangesAsync(cancellationToken);

        return OnboardingChecklistMapper.ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _templateRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Onboarding checklist template was not found.");

        var hasChecklists = await _checklistRepository.GetQueryable()
            .AnyAsync(c => c.TemplateId == id, cancellationToken);
        if (hasChecklists)
            throw new BusinessRuleException("Onboarding checklist template cannot be deleted because employees reference it.");

        _templateRepository.Remove(entity);
        await _templateRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<OnboardingChecklistTemplate?> LoadTemplateWithItemsAsync(int id, CancellationToken cancellationToken)
    {
        return await _templateRepository.GetQueryable()
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    private async Task EnsureUniqueNameAsync(
        int organizationId,
        string name,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicate = await _templateRepository.GetQueryable()
            .AnyAsync(
                t => t.OrganizationId == organizationId
                     && t.Name == name
                     && (!excludeId.HasValue || t.Id != excludeId.Value),
                cancellationToken);
        if (duplicate)
            throw new BusinessRuleException("An onboarding checklist template with the same name already exists.");
    }

    private async Task ClearDefaultFlagAsync(int organizationId, int? excludeId, CancellationToken cancellationToken)
    {
        var defaults = await _templateRepository.GetQueryable()
            .Where(t => t.OrganizationId == organizationId && t.IsDefault && (!excludeId.HasValue || t.Id != excludeId.Value))
            .ToListAsync(cancellationToken);

        foreach (var row in defaults)
        {
            row.IsDefault = false;
            row.UpdatedAtUtc = DateTime.UtcNow;
            _templateRepository.Update(row);
        }
    }

    private static void ValidateItems(IReadOnlyList<OnboardingChecklistTemplateItemRequestModel> items)
    {
        if (items.Count == 0)
            throw new BusinessRuleException("At least one checklist item is required.");

        foreach (var item in items)
        {
            StringHelper.NormalizeRequired(item.Title);
            if (!Enum.IsDefined(typeof(OnboardingChecklistItemCategory), item.Category))
                throw new BusinessRuleException("Checklist item category is invalid.");
            if (item.DefaultDueDaysFromStart.HasValue && item.DefaultDueDaysFromStart.Value < 0)
                throw new BusinessRuleException("Default due days from start cannot be negative.");
        }
    }

    private async Task SyncItemsAsync(
        OnboardingChecklistTemplate entity,
        IReadOnlyList<OnboardingChecklistTemplateItemRequestModel> requestedItems,
        CancellationToken cancellationToken)
    {
        var requestedIds = requestedItems.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        var existingById = entity.Items.ToDictionary(i => i.Id);

        foreach (var existing in entity.Items.ToList())
        {
            if (requestedIds.Contains(existing.Id))
                continue;

            var hasGeneratedTasks = await _itemRepository.GetQueryable()
                .Where(i => i.Id == existing.Id)
                .SelectMany(i => i.GeneratedTasks)
                .AnyAsync(cancellationToken);
            if (hasGeneratedTasks)
                throw new BusinessRuleException($"Checklist item '{existing.Title}' cannot be removed because generated tasks reference it.");

            entity.Items.Remove(existing);
            _itemRepository.Remove(existing);
        }

        foreach (var request in requestedItems.OrderBy(i => i.SortOrder))
        {
            if (request.Id.HasValue && existingById.TryGetValue(request.Id.Value, out var existing))
            {
                OnboardingChecklistMapper.ApplyItemUpdate(existing, request);
                _itemRepository.Update(existing);
                continue;
            }

            var created = OnboardingChecklistMapper.ToItemEntity(request, entity.Id);
            entity.Items.Add(created);
            await _itemRepository.AddAsync(created, cancellationToken);
        }
    }
}
