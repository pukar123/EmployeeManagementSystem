using EMS.Application.DTOs.Leave;
using Pukar.Shared;

namespace EMS.Application.Services.Leave;

public sealed class LeaveImportService : ILeaveImportService
{
    private readonly ILeaveRequestService _leaveRequestService;

    public LeaveImportService(ILeaveRequestService leaveRequestService)
    {
        _leaveRequestService = leaveRequestService;
    }

    public async Task<BulkLeaveImportResultResponseModel> ImportAsync(
        BulkLeaveImportRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var rowResults = new List<BulkLeaveImportRowResultModel>(request.Items.Count);
        var imported = 0;

        for (var i = 0; i < request.Items.Count; i++)
        {
            var rowNumber = i + 1;
            var item = request.Items[i];
            try
            {
                var created = await _leaveRequestService.CreateAsync(
                    new CreateLeaveRequestRequestModel
                    {
                        EmployeeId = item.EmployeeId,
                        LeaveTypeId = item.LeaveTypeId,
                        StartDateUtc = item.StartDateUtc,
                        EndDateUtc = item.EndDateUtc,
                        Unit = item.Unit,
                        RequestedAmount = item.RequestedAmount,
                        Reason = item.Reason,
                    },
                    cancellationToken);

                imported++;
                rowResults.Add(new BulkLeaveImportRowResultModel
                {
                    RowNumber = rowNumber,
                    Success = true,
                    LeaveRequestId = created.Id,
                });
            }
            catch (BusinessRuleException ex)
            {
                rowResults.Add(new BulkLeaveImportRowResultModel
                {
                    RowNumber = rowNumber,
                    Success = false,
                    Error = ex.Message,
                });
            }
        }

        return new BulkLeaveImportResultResponseModel
        {
            ImportKey = request.ImportKey,
            TotalRows = request.Items.Count,
            ImportedRows = imported,
            FailedRows = request.Items.Count - imported,
            RowResults = rowResults,
        };
    }
}
