using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// Base class for access-test fixtures: one in-memory API and one seeded database per fixture.
/// Tests in a fixture share the database, so a test that changes data must create its own rows.
/// </summary>
public abstract class AccessTestFixture
{
    private const int BodyPreviewLength = 400;

    // The API uses the ASP.NET Core defaults: camelCase names, case-insensitive reading, enums as numbers.
    private static readonly JsonSerializerOptions ApiJson = new(JsonSerializerDefaults.Web);

    private EmsApiFactory? _factory;

    protected EmsApiFactory Factory
        => _factory ?? throw new InvalidOperationException("The API has not been started for this fixture.");

    /// <summary>
    /// Extra host settings for this fixture's API, applied after the harness defaults.
    /// Override in a fixture that needs different settings (for example low rate limits).
    /// </summary>
    protected virtual IReadOnlyDictionary<string, string?>? HostSettings => null;

    [OneTimeSetUp]
    public async Task StartApiAsync()
    {
        _factory = new EmsApiFactory(HostSettings);
        await _factory.InitialiseAsync();
    }

    [OneTimeTearDown]
    public async Task StopApiAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    /// <summary>
    /// Sends the request as <paramref name="user"/> and asserts the status code.
    /// <paramref name="body"/>, when given, is sent as JSON in the same shape the web client sends.
    /// Returns the response body so the test can check fields in it.
    /// </summary>
    protected async Task<string> ExpectStatusAsync(
        TestUser user,
        HttpMethod method,
        string path,
        HttpStatusCode expected,
        object? body = null)
    {
        using var client = Factory.CreateClientAs(user);
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: ApiJson);

        using var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.That(
            response.StatusCode,
            Is.EqualTo(expected),
            $"{user.Description} -> {method} {path}{(body is null ? string.Empty : " (with JSON body)")}: " +
            $"expected {(int)expected} {expected}, got {(int)response.StatusCode} {response.StatusCode}. " +
            $"Body starts: {Preview(responseBody)}");

        return responseBody;
    }

    /// <summary>
    /// One row for a <c>[TestCaseSource]</c> status test: user, method and path, expected status, and an
    /// optional JSON body. The test method receives (user, method, path, expected, body).
    /// </summary>
    protected static TestCaseData Case(
        string id,
        TestUser user,
        HttpMethod method,
        string path,
        HttpStatusCode expected,
        object? body = null)
        => new TestCaseData(user, method, path, expected, body)
            .SetName($"{id} {TestName(user.Description)} {method} {TestName(path)} returns {(int)expected}");

    /// <summary>Parses a JSON response body, failing with a readable message if it is not JSON.</summary>
    protected static JsonElement ParseJson(string body, TestUser user, HttpMethod method, string path)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            Assert.Fail($"{user.Description} -> {method} {path}: response is not JSON ({ex.Message}). Body starts: {Preview(body)}");
            throw;
        }
    }

    /// <summary>Failure message for a body check: who, what, expected, actual and the body start.</summary>
    protected static string BodyMessage(
        TestUser user, HttpMethod method, string path, string field, string expected, string body)
        => $"{user.Description} -> {method} {path}: expected {field} {expected}. Body starts: {Preview(body)}";

    // Test names avoid brackets and dots, which test runners read as parameters and namespaces.
    private static string TestName(string text)
        => text.Replace("(", string.Empty).Replace(")", string.Empty).Replace(".", "_");

    private static string Preview(string body)
    {
        if (string.IsNullOrEmpty(body))
            return "(empty)";

        return body.Length <= BodyPreviewLength ? body : body[..BodyPreviewLength] + "...";
    }
}
