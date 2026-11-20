using EMS.Application.Services.Employees;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// Test-only wrapper around the real <see cref="EmployeeRoleSyncService"/>: records each call in
/// <see cref="RoleSyncRecorder"/>, then hands it to the real service so behaviour is unchanged.
/// The call is recorded before it runs, so a sync that starts and then fails is still seen.
/// </summary>
internal sealed class RecordingEmployeeRoleSyncService : IEmployeeRoleSyncService
{
    private readonly EmployeeRoleSyncService _inner;
    private readonly RoleSyncRecorder _recorder;

    public RecordingEmployeeRoleSyncService(EmployeeRoleSyncService inner, RoleSyncRecorder recorder)
    {
        _inner = inner;
        _recorder = recorder;
    }

    public Task SyncEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        _recorder.Record(RoleSyncRecorder.SyncEmployeeMethod, employeeId);
        return _inner.SyncEmployeeAsync(employeeId, cancellationToken);
    }

    public Task SyncEmployeesForPositionAsync(int jobPositionId, CancellationToken cancellationToken = default)
    {
        _recorder.Record(RoleSyncRecorder.SyncEmployeesForPositionMethod, jobPositionId);
        return _inner.SyncEmployeesForPositionAsync(jobPositionId, cancellationToken);
    }
}
