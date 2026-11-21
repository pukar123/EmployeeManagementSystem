using System.Net;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// What an anonymous auth call returned, read in full so the response can be disposed straight away.
/// </summary>
/// <param name="Description">Plain description of the call for failure messages, e.g. "login #4 from 10.5.0.5".</param>
/// <param name="StatusCode">The response status.</param>
/// <param name="Body">The response body (may be empty).</param>
/// <param name="Headers">Response and content headers, names case-insensitive, repeated values joined with ", ".</param>
public sealed record AuthCallResult(
    string Description,
    HttpStatusCode StatusCode,
    string Body,
    IReadOnlyDictionary<string, string> Headers)
{
    /// <summary>The header's value, or null when the response does not have it.</summary>
    public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;

    public override string ToString() => $"{Description}: {(int)StatusCode} {StatusCode}";
}
