using System.Net;
using System.Text.Json;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// Base class for access-test fixtures: one in-memory API and one seeded database per fixture.
/// Tests in a fixture share the database, so a test that changes data must create its own rows.
/// </summary>
public abstract class AccessTestFixture
{
    private const int BodyPreviewLength = 400;

    private EmsApiFactory? _factory;

    protected EmsApiFactory Factory
        => _factory ?? throw new InvalidOperationException("The API has not been started for this fixture.");

    [OneTimeSetUp]
    public async Task StartApiAsync()
    {
        _factory = new EmsApiFactory();
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
    /// Returns the response body so the test can check fields in it.
    /// </summary>
    protected async Task<string> ExpectStatusAsync(
        TestUser user,
        HttpMethod method,
        string path,
        HttpStatusCode expected)
    {
        using var client = Factory.CreateClientAs(user);
        using var request = new HttpRequestMessage(method, path);
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(
            response.StatusCode,
            Is.EqualTo(expected),
            $"{user.Description} -> {method} {path}: expected {(int)expected} {expected}, " +
            $"got {(int)response.StatusCode} {response.StatusCode}. Body starts: {Preview(body)}");

        return body;
    }

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

    private static string Preview(string body)
    {
        if (string.IsNullOrEmpty(body))
            return "(empty)";

        return body.Length <= BodyPreviewLength ? body : body[..BodyPreviewLength] + "...";
    }
}
