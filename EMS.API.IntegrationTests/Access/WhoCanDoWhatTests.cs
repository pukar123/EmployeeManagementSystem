using System.Net;
using System.Text.Json;
using EMS.API.IntegrationTests.Harness;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// "Who can do what": each row is user -> method and path -> expected status, run against the real
/// API with real tokens. Add a row here for every access rule a security task fixes or relies on.
/// </summary>
[TestFixture]
public sealed class WhoCanDoWhatTests : AccessTestFixture
{
    private static readonly string EmployeePortal = "/api/EmployeePortal";
    private static readonly string AdminMenuAccess = "/api/RoleKeyPermissions/ADMIN";
    private static readonly string ManagerTeamInO1 = $"/api/Manager/team?organizationId={TestSeed.O1.Id}";

    private static string EmployeeProfile(SeededEmployee employee) => $"/api/Employees/{employee.EmployeeId}/profile";

    private static IEnumerable<TestCaseData> StatusCases()
    {
        yield return Case("C1", TestUsers.Anonymous, HttpMethod.Get, EmployeePortal, HttpStatusCode.Unauthorized);
        yield return Case("C2", TestUsers.AdminWithUntrustedSigningKey, HttpMethod.Get, AdminMenuAccess, HttpStatusCode.Unauthorized);
        yield return Case("C3", TestUsers.Employee(TestSeed.E1), HttpMethod.Get, AdminMenuAccess, HttpStatusCode.OK);
        yield return Case("C7", TestUsers.Employee(TestSeed.E1), HttpMethod.Get, EmployeeProfile(TestSeed.E2), HttpStatusCode.Forbidden);
        yield return Case("C8", TestUsers.Employee(TestSeed.E1), HttpMethod.Get, ManagerTeamInO1, HttpStatusCode.Forbidden);
        yield return Case("C9", TestUsers.Manager(TestSeed.M1), HttpMethod.Get, EmployeeProfile(TestSeed.E1), HttpStatusCode.OK);
        yield return Case("C10", TestUsers.Manager(TestSeed.M1), HttpMethod.Get, EmployeeProfile(TestSeed.E2), HttpStatusCode.Forbidden);
        yield return Case("C11", TestUsers.Manager(TestSeed.M1), HttpMethod.Get, ManagerTeamInO1, HttpStatusCode.OK);
    }

    [TestCaseSource(nameof(StatusCases))]
    public async Task Request_ReturnsExpectedStatus(TestUser user, HttpMethod method, string path, HttpStatusCode expected)
    {
        await ExpectStatusAsync(user, method, path, expected);
    }

    [Test]
    public async Task C4_AdminReadsAdminMenuAccess_ReturnsSeededAdminMenus()
    {
        var user = TestUsers.Admin;
        var body = await ExpectStatusAsync(user, HttpMethod.Get, AdminMenuAccess, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, AdminMenuAccess);

        Assert.Multiple(() =>
        {
            Assert.That(
                json.TryGetProperty("roleKey", out var roleKey) ? roleKey.GetString() : null,
                Is.EqualTo(TestSeed.AdminRoleKey),
                BodyMessage(user, HttpMethod.Get, AdminMenuAccess, "roleKey", TestSeed.AdminRoleKey, body));
            Assert.That(
                json.TryGetProperty("menuIds", out var menuIds) && menuIds.ValueKind == JsonValueKind.Array
                    ? menuIds.GetArrayLength()
                    : 0,
                Is.GreaterThan(0),
                BodyMessage(user, HttpMethod.Get, AdminMenuAccess, "menuIds", "to be a non-empty list", body));
        });
    }

    [Test]
    public async Task C5_LinkedEmployeeOpensPortal_SeesOwnProfile()
    {
        var user = TestUsers.Employee(TestSeed.E1);
        var body = await ExpectStatusAsync(user, HttpMethod.Get, EmployeePortal, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, EmployeePortal);

        Assert.Multiple(() =>
        {
            Assert.That(
                json.TryGetProperty("hasLinkedEmployeeProfile", out var linked) && linked.ValueKind == JsonValueKind.True,
                Is.True,
                BodyMessage(user, HttpMethod.Get, EmployeePortal, "hasLinkedEmployeeProfile", "true", body));
            Assert.That(
                json.TryGetProperty("employeeId", out var employeeId) && employeeId.ValueKind == JsonValueKind.Number
                    ? employeeId.GetInt32()
                    : (int?)null,
                Is.EqualTo(TestSeed.E1.EmployeeId),
                BodyMessage(user, HttpMethod.Get, EmployeePortal, "employeeId", $"{TestSeed.E1.EmployeeId} (E1)", body));
        });
    }

    [Test]
    public async Task C6_UnlinkedUserOpensPortal_SeesNoProfile()
    {
        var user = TestUsers.NoRole;
        var body = await ExpectStatusAsync(user, HttpMethod.Get, EmployeePortal, HttpStatusCode.OK);
        var json = ParseJson(body, user, HttpMethod.Get, EmployeePortal);

        Assert.Multiple(() =>
        {
            Assert.That(
                json.TryGetProperty("hasLinkedEmployeeProfile", out var linked) && linked.ValueKind == JsonValueKind.False,
                Is.True,
                BodyMessage(user, HttpMethod.Get, EmployeePortal, "hasLinkedEmployeeProfile", "false", body));
            Assert.That(
                !json.TryGetProperty("employeeId", out var employeeId) || employeeId.ValueKind == JsonValueKind.Null,
                Is.True,
                BodyMessage(user, HttpMethod.Get, EmployeePortal, "employeeId", "to be missing or null", body));
        });
    }

    private static TestCaseData Case(string id, TestUser user, HttpMethod method, string path, HttpStatusCode expected)
        => new TestCaseData(user, method, path, expected)
            .SetName($"{id} {TestName(user.Description)} {method} {TestName(path)} returns {(int)expected}");

    // Test names avoid brackets and dots, which test runners read as parameters and namespaces.
    private static string TestName(string text)
        => text.Replace("(", string.Empty).Replace(")", string.Empty).Replace(".", "_");
}
