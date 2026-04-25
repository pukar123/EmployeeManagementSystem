using EMS.Application.DTOs.Task;

namespace EMS.Application.Services.Tasks;

public interface ITaskService
{
    Task<TaskResponseModel> CreateAsync(CreateTaskRequestModel request, int? assignedByUserId, CancellationToken cancellationToken = default);
    Task<TaskResponseModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskResponseModel>> GetAllAsync(
        int? employeeId = null,
        int? assignedByUserId = null,
        DateTime? rangeStartUtc = null,
        DateTime? rangeEndUtc = null,
        CancellationToken cancellationToken = default);
    Task<TaskResponseModel?> UpdateAsync(int id, UpdateTaskRequestModel request, CancellationToken cancellationToken = default);
    Task<TaskResponseModel?> UpdateStatusAsync(int id, UpdateTaskStatusRequestModel request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
