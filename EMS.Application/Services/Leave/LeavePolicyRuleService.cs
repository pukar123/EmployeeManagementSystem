using EMS.Application.DTOs.Leave;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeavePolicyRuleService : ILeavePolicyRuleService
{
    private readonly ILeavePolicyRuleRepository _policyRuleRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public LeavePolicyRuleService(
        ILeavePolicyRuleRepository policyRuleRepository,
        ILeaveTypeRepository leaveTypeRepository)
    {
        _policyRuleRepository = policyRuleRepository;
        _leaveTypeRepository = leaveTypeRepository;
    }

    public async Task<IReadOnlyList<LeavePolicyRuleResponseModel>> GetByLeaveTypeAsync(
        int leaveTypeId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _policyRuleRepository.GetByLeaveTypeAsync(leaveTypeId, cancellationToken);
        return rows.Select(LeaveMapper.ToResponse).ToList();
    }

    public async Task<LeavePolicyRuleResponseModel> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _policyRuleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Leave policy rule was not found.");
        return LeaveMapper.ToResponse(entity);
    }

    public async Task<LeavePolicyRuleResponseModel> CreateAsync(
        CreateLeavePolicyRuleRequestModel request,
        CancellationToken cancellationToken = default)
    {
        await ValidateLeaveTypeAsync(request.LeaveTypeId, request.OrganizationId, cancellationToken);
        ValidateDateRange(request.EffectiveFromDateUtc, request.EffectiveToDateUtc);

        var entity = new LeavePolicyRule
        {
            OrganizationId = request.OrganizationId,
            LeaveTypeId = request.LeaveTypeId,
            AccrualRatePerPeriod = request.AccrualRatePerPeriod,
            AccrualFrequency = request.AccrualFrequency,
            MaximumCarryForward = request.MaximumCarryForward,
            EnableProration = request.EnableProration,
            IsActive = request.IsActive,
            EffectiveFromDateUtc = request.EffectiveFromDateUtc.Date,
            EffectiveToDateUtc = request.EffectiveToDateUtc?.Date,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        await _policyRuleRepository.AddAsync(entity, cancellationToken);
        await _policyRuleRepository.SaveChangesAsync(cancellationToken);
        return LeaveMapper.ToResponse(entity);
    }

    public async Task<LeavePolicyRuleResponseModel> UpdateAsync(
        int id,
        UpdateLeavePolicyRuleRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _policyRuleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Leave policy rule was not found.");

        ValidateDateRange(request.EffectiveFromDateUtc, request.EffectiveToDateUtc);

        entity.AccrualRatePerPeriod = request.AccrualRatePerPeriod;
        entity.AccrualFrequency = request.AccrualFrequency;
        entity.MaximumCarryForward = request.MaximumCarryForward;
        entity.EnableProration = request.EnableProration;
        entity.IsActive = request.IsActive;
        entity.EffectiveFromDateUtc = request.EffectiveFromDateUtc.Date;
        entity.EffectiveToDateUtc = request.EffectiveToDateUtc?.Date;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _policyRuleRepository.Update(entity);
        await _policyRuleRepository.SaveChangesAsync(cancellationToken);
        return LeaveMapper.ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _policyRuleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleException("Leave policy rule was not found.");

        _policyRuleRepository.Remove(entity);
        await _policyRuleRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateLeaveTypeAsync(int leaveTypeId, int organizationId, CancellationToken cancellationToken)
    {
        var leaveType = await _leaveTypeRepository.GetByIdAsync(leaveTypeId, cancellationToken)
            ?? throw new BusinessRuleException("Leave type was not found.");
        if (leaveType.OrganizationId != organizationId)
            throw new BusinessRuleException("Policy organization and leave type organization must match.");
    }

    private static void ValidateDateRange(DateTime from, DateTime? to)
    {
        if (to.HasValue && to.Value.Date < from.Date)
            throw new BusinessRuleException("EffectiveToDateUtc cannot be earlier than EffectiveFromDateUtc.");
    }
}
