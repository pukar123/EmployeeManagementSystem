using System.Collections.Concurrent;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// Test-only log of every role sync the API starts. One per test host (registered as a singleton),
/// so a test can check whether a request did or did not push role changes to employees.
/// </summary>
public sealed class RoleSyncRecorder
{
    public const string SyncEmployeeMethod = "SyncEmployeeAsync";
    public const string SyncEmployeesForPositionMethod = "SyncEmployeesForPositionAsync";

    private readonly ConcurrentQueue<RoleSyncCall> _calls = new();

    /// <summary>The calls recorded so far, oldest first.</summary>
    public IReadOnlyList<RoleSyncCall> Calls => _calls.ToArray();

    internal void Record(string method, int id) => _calls.Enqueue(new RoleSyncCall(method, id));
}
