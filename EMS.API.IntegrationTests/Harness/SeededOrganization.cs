namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// An organisation created by <see cref="TestSeed"/>. Refer to it through <see cref="TestSeed"/>, never by number.
/// </summary>
public sealed record SeededOrganization(string Label, int Id, string Name);
