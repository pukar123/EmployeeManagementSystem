namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// An employee created by <see cref="TestSeed"/>.
/// <see cref="UserId"/> is stored in the employee's <c>ExternalIdentityKey</c>, so a token whose
/// <c>sub</c> is this user id is "linked" to this employee.
/// </summary>
public sealed record SeededEmployee(string Label, int EmployeeId, int UserId, string FirstName, string LastName);
