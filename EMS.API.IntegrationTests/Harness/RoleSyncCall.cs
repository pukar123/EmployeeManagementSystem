namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// One call made to <c>IEmployeeRoleSyncService</c> during a test, as seen by <see cref="RoleSyncRecorder"/>.
/// </summary>
/// <param name="Method">The service method that was called, e.g. <c>SyncEmployeesForPositionAsync</c>.</param>
/// <param name="Id">The job position id or employee id it was called with.</param>
public sealed record RoleSyncCall(string Method, int Id);
