using EMS.Application.DTOs.Leave;

namespace EMS.Application.Services.Leave;

public interface ILeaveImportService
{
    Task<BulkLeaveImportResultResponseModel> ImportAsync(
        BulkLeaveImportRequestModel request,
        CancellationToken cancellationToken = default);
}
