using EMS.Application.DTOs.Leave;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;

namespace EMS.Application.Mapping;

internal static class LeaveMapper
{
    public static LeaveTypeResponseModel ToResponse(LeaveType entity)
    {
        return new LeaveTypeResponseModel
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            Name = entity.Name,
            Description = entity.Description,
            Unit = entity.Unit,
            RequiresAttachment = entity.RequiresAttachment,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }

    public static LeaveBalanceResponseModel ToResponse(LeaveBalance entity)
    {
        var availableAmount = entity.OpeningBalance
            + entity.AccruedAmount
            + entity.AdjustedAmount
            + entity.CarryForwardAmount
            - entity.UsedAmount;

        return new LeaveBalanceResponseModel
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            EmployeeId = entity.EmployeeId,
            LeaveTypeId = entity.LeaveTypeId,
            OpeningBalance = entity.OpeningBalance,
            AccruedAmount = entity.AccruedAmount,
            UsedAmount = entity.UsedAmount,
            AdjustedAmount = entity.AdjustedAmount,
            CarryForwardAmount = entity.CarryForwardAmount,
            AvailableAmount = decimal.Round(availableAmount, 2),
            BalanceAsOfUtc = entity.BalanceAsOfUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }

    public static LeaveRequestResponseModel ToResponse(LeaveRequest entity)
    {
        return new LeaveRequestResponseModel
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            EmployeeId = entity.EmployeeId,
            LeaveTypeId = entity.LeaveTypeId,
            StartDateUtc = entity.StartDateUtc,
            EndDateUtc = entity.EndDateUtc,
            Unit = entity.Unit,
            RequestedAmount = entity.RequestedAmount,
            Reason = entity.Reason,
            Status = entity.Status,
            SubmittedAtUtc = entity.SubmittedAtUtc,
            ReviewedAtUtc = entity.ReviewedAtUtc,
            ReviewedByEmployeeId = entity.ReviewedByEmployeeId,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }

    public static LeavePolicyRuleResponseModel ToResponse(LeavePolicyRule entity)
    {
        return new LeavePolicyRuleResponseModel
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            LeaveTypeId = entity.LeaveTypeId,
            AccrualRatePerPeriod = entity.AccrualRatePerPeriod,
            AccrualFrequency = entity.AccrualFrequency,
            MaximumCarryForward = entity.MaximumCarryForward,
            EnableProration = entity.EnableProration,
            IsActive = entity.IsActive,
            EffectiveFromDateUtc = entity.EffectiveFromDateUtc,
            EffectiveToDateUtc = entity.EffectiveToDateUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
    }

    public static LeaveRequest CreateRequestEntity(
        CreateLeaveRequestRequestModel request,
        int organizationId)
    {
        return new LeaveRequest
        {
            OrganizationId = organizationId,
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDateUtc = request.StartDateUtc.Date,
            EndDateUtc = request.EndDateUtc.Date,
            Unit = request.Unit,
            RequestedAmount = request.RequestedAmount,
            Reason = request.Reason,
            Status = LeaveRequestStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }
}
