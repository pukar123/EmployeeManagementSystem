using System.Globalization;
using System.Net;
using EMS.API.IntegrationTests.Harness;
using EMS.Application.DTOs.Leave;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// B11: the leave-year reset, through <c>POST /api/LeaveOperations/year-reset/run</c>, carries each balance's
/// leftover leave into the new year once, capped by the leave type's carry-forward limit. The carry is held only in
/// <c>CarryForwardAmount</c> (opening is 0), so the available amount the balance endpoint returns equals the carry,
/// and running the reset again with nothing in between changes no amount.
/// This fixture has its own database with <see cref="LeaveYearResetSeed"/> rows in an organisation of its own.
/// </summary>
[TestFixture]
public sealed class LeaveYearResetTests : AccessTestFixture
{
    private const string YearResetRun = "/api/LeaveOperations/year-reset/run";

    private static readonly RunLeaveYearResetRequestModel ResetRequest = new()
    {
        OrganizationId = LeaveYearResetSeed.OrganizationId,
        LeaveYearStartDateUtc = LeaveYearResetSeed.LeaveYearStart,
    };

    // Balance id, employee, leave type, expected carry (= expected available after the reset).
    private static readonly (int BalanceId, SeededEmployee Employee, int LeaveTypeId, decimal ExpectedCarry)[] Expected =
    [
        (LeaveYearResetSeed.BalanceR1Capped, LeaveYearResetSeed.R1, LeaveYearResetSeed.CappedLeaveTypeId, 10m),
        (LeaveYearResetSeed.BalanceR2Negative, LeaveYearResetSeed.R2, LeaveYearResetSeed.CappedLeaveTypeId, 0m),
        (LeaveYearResetSeed.BalanceR3Uncapped, LeaveYearResetSeed.R3, LeaveYearResetSeed.UncappedLeaveTypeId, 15m),
    ];

    [OneTimeSetUp]
    public Task AddYearResetRowsAsync()
        => Factory.ChangeDatabaseAsync(LeaveYearResetSeed.AddRowsAsync);

    [Test]
    [Order(1)]
    public async Task I1_YearReset_CarriesEachBalanceOnce_CappedByTheLimit()
    {
        await RunResetAsync();

        await AssertBalancesAfterResetAsync();
    }

    [Test]
    [Order(2)]
    public async Task I2_YearReset_RunAgainWithNoChanges_LeavesEveryAmountTheSame()
    {
        // Runs the reset itself first, so this test does not depend on I1 having run.
        await RunResetAsync();
        await AssertBalancesAfterResetAsync();

        await RunResetAsync();

        await AssertBalancesAfterResetAsync();
    }

    private async Task RunResetAsync()
    {
        var admin = TestUsers.Admin;
        var body = await ExpectStatusAsync(admin, HttpMethod.Post, YearResetRun, HttpStatusCode.OK, ResetRequest);
        var json = ParseJson(body, admin, HttpMethod.Post, YearResetRun);

        Assert.That(json.GetProperty("balancesReset").GetInt32(), Is.EqualTo(3), "balancesReset");
        Assert.That(json.GetProperty("organizationId").GetInt32(), Is.EqualTo(LeaveYearResetSeed.OrganizationId));
    }

    private async Task AssertBalancesAfterResetAsync()
    {
        var ids = Expected.Select(x => x.BalanceId).ToArray();
        var rows = await Factory.QueryDatabaseAsync(db => db.LeaveBalances.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id));

        foreach (var (balanceId, employee, leaveTypeId, expectedCarry) in Expected)
        {
            var row = rows[balanceId];
            Assert.Multiple(() =>
            {
                Assert.That(row.OpeningBalance, Is.EqualTo(0m), $"{employee.Label} OpeningBalance");
                Assert.That(row.AccruedAmount, Is.EqualTo(0m), $"{employee.Label} AccruedAmount");
                Assert.That(row.UsedAmount, Is.EqualTo(0m), $"{employee.Label} UsedAmount");
                Assert.That(row.AdjustedAmount, Is.EqualTo(0m), $"{employee.Label} AdjustedAmount");
                Assert.That(row.CarryForwardAmount, Is.EqualTo(expectedCarry), $"{employee.Label} CarryForwardAmount");
                Assert.That(row.BalanceAsOfUtc, Is.EqualTo(LeaveYearResetSeed.LeaveYearStart), $"{employee.Label} BalanceAsOfUtc");
            });

            // The available amount the portal shows, from the balance endpoint.
            var admin = TestUsers.Admin;
            var path = string.Create(
                CultureInfo.InvariantCulture,
                $"/api/LeaveBalances/employee/{employee.EmployeeId}/type/{leaveTypeId}");
            var body = await ExpectStatusAsync(admin, HttpMethod.Get, path, HttpStatusCode.OK);
            var json = ParseJson(body, admin, HttpMethod.Get, path);
            Assert.That(
                json.GetProperty("availableAmount").GetDecimal(),
                Is.EqualTo(expectedCarry),
                $"{employee.Label} availableAmount");
        }
    }
}
