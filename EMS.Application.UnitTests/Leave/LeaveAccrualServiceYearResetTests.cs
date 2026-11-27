using EMS.Application.DTOs.Leave;
using EMS.Application.Mapping;
using EMS.Application.Services.Leave;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using MockQueryable;
using Moq;

namespace EMS.Application.UnitTests.Leave;

/// <summary>
/// B11: the leave-year reset carries each balance's leftover leave into the new year once, capped by the leave type's
/// carry-forward limit. After the reset the carry is held only in <see cref="LeaveBalance.CarryForwardAmount"/>, so the
/// available balance equals the carry (cases U1 to U14 in the B11 requirements).
/// </summary>
public class LeaveAccrualServiceYearResetTests
{
    private const int OrganizationId = 1;
    private const int OtherOrganizationId = 2;
    private const int LeaveTypeWithRule = 3;
    private const int LeaveTypeWithoutRule = 4;

    private static readonly DateTime LeaveYearStart = new(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private List<LeavePolicyRule> _rules = null!;
    private List<LeaveBalance> _balances = null!;
    private Mock<ILeavePolicyRuleRepository> _policyRepository = null!;
    private Mock<ILeaveBalanceRepository> _balanceRepository = null!;
    private Mock<IBaseRepository<Employee>> _employeeRepository = null!;

    [SetUp]
    public void SetUp()
    {
        _rules = [];
        _balances = [];

        _policyRepository = new Mock<ILeavePolicyRuleRepository>();
        _policyRepository.Setup(x => x.GetQueryable()).Returns(() => _rules.BuildMock());

        _balanceRepository = new Mock<ILeaveBalanceRepository>();
        _balanceRepository.Setup(x => x.GetQueryable()).Returns(() => _balances.BuildMock());
        _balanceRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _employeeRepository = new Mock<IBaseRepository<Employee>>();
    }

    private LeaveAccrualService CreateSut() => new(
        _policyRepository.Object,
        _balanceRepository.Object,
        _employeeRepository.Object);

    private static RunLeaveYearResetRequestModel ResetRequest(int organizationId = OrganizationId) => new()
    {
        OrganizationId = organizationId,
        LeaveYearStartDateUtc = LeaveYearStart,
    };

    // Year-end O/A/U/Adj/C, the limit (null with a rule means "rule with no limit"), whether the leave type has a rule,
    // and the expected carry. Available after the reset must equal the carry.
    private static IEnumerable<TestCaseData> ResetCases()
    {
        yield return Case("U1 no limit", 0m, 20m, 5m, 0m, 0m, null, hasRule: true, expectedCarry: 15m);
        yield return Case("U2 limit lower", 0m, 20m, 5m, 0m, 0m, 10m, hasRule: true, expectedCarry: 10m);
        yield return Case("U3 limit higher", 0m, 20m, 5m, 0m, 0m, 40m, hasRule: true, expectedCarry: 15m);
        yield return Case("U4 zero limit", 0m, 20m, 5m, 0m, 0m, 0m, hasRule: true, expectedCarry: 0m);
        yield return Case("U5 zero balance", 0m, 5m, 5m, 0m, 0m, null, hasRule: true, expectedCarry: 0m);
        yield return Case("U6 negative balance", 0m, 2m, 5m, 0m, 0m, null, hasRule: true, expectedCarry: 0m);
        yield return Case("U6b negative with limit", 0m, 2m, 5m, 0m, 0m, 10m, hasRule: true, expectedCarry: 0m);
        yield return Case("U7 used leave with previous carry", 0m, 20m, 12m, 0m, 10m, 10m, hasRule: true, expectedCarry: 10m);
        yield return Case("U8 adjustment included", 0m, 10m, 4m, 2.5m, 0m, null, hasRule: true, expectedCarry: 8.5m);
        yield return Case("U9 pre-fix doubled row is not repaired", 10m, 0m, 0m, 0m, 10m, null, hasRule: true, expectedCarry: 20m);
        yield return Case("U10 leave type with no policy", 0m, 7m, 1m, 0m, 0m, null, hasRule: false, expectedCarry: 6m);
    }

    private static TestCaseData Case(
        string name,
        decimal opening,
        decimal accrued,
        decimal used,
        decimal adjusted,
        decimal carry,
        decimal? limit,
        bool hasRule,
        decimal expectedCarry)
        => new TestCaseData(opening, accrued, used, adjusted, carry, limit, hasRule, expectedCarry).SetName(name);

    [TestCaseSource(nameof(ResetCases))]
    public async Task RunLeaveYearResetAsync_CarriesLeftoverOnce(
        decimal opening,
        decimal accrued,
        decimal used,
        decimal adjusted,
        decimal carry,
        decimal? limit,
        bool hasRule,
        decimal expectedCarry)
    {
        var leaveTypeId = hasRule ? LeaveTypeWithRule : LeaveTypeWithoutRule;
        if (hasRule)
            _rules.Add(Rule(1, LeaveTypeWithRule, limit, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));
        var balance = Balance(1, OrganizationId, leaveTypeId, opening, accrued, used, adjusted, carry);
        _balances.Add(balance);
        var startedAtUtc = DateTime.UtcNow;

        var result = await CreateSut().RunLeaveYearResetAsync(ResetRequest());

        AssertReset(balance, expectedCarry);
        Assert.That(balance.UpdatedAtUtc, Is.GreaterThanOrEqualTo(startedAtUtc));
        Assert.That(result.BalancesReset, Is.EqualTo(1));
        Assert.That(result.OrganizationId, Is.EqualTo(OrganizationId));
        Assert.That(result.LeaveYearStartDateUtc, Is.EqualTo(LeaveYearStart));
        _balanceRepository.Verify(x => x.Update(balance), Times.Once);
        _balanceRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(10, 10, TestName = "U11 re-run with no changes, limit 10")]
    [TestCase(null, 15, TestName = "U12 re-run with no changes, no limit")]
    public async Task RunLeaveYearResetAsync_IsStable_WhenRunAgainWithNoChanges(int? limit, int expectedCarry)
    {
        _rules.Add(Rule(1, LeaveTypeWithRule, limit, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));
        var balance = Balance(1, OrganizationId, LeaveTypeWithRule, 0m, 20m, 5m, 0m, 0m);
        _balances.Add(balance);
        var sut = CreateSut();

        await sut.RunLeaveYearResetAsync(ResetRequest());
        AssertReset(balance, expectedCarry);

        var second = await sut.RunLeaveYearResetAsync(ResetRequest());

        AssertReset(balance, expectedCarry);
        Assert.That(second.BalancesReset, Is.EqualTo(1));
        _balanceRepository.Verify(x => x.Update(balance), Times.Exactly(2));
        _balanceRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task RunLeaveYearResetAsync_ChangesOnlyTheRequestedOrganisation()
    {
        // U13 (AC10): the other organisation's balance (and its rule) are left exactly as they were.
        _rules.Add(Rule(1, LeaveTypeWithRule, 10m, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));
        var ownBalance = Balance(1, OrganizationId, LeaveTypeWithRule, 0m, 20m, 5m, 0m, 0m);
        var otherAsOf = new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var otherUpdatedAt = new DateTime(2027, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var otherBalance = Balance(2, OtherOrganizationId, LeaveTypeWithRule, 1m, 20m, 5m, 2m, 3m);
        otherBalance.BalanceAsOfUtc = otherAsOf;
        otherBalance.UpdatedAtUtc = otherUpdatedAt;
        _balances.Add(ownBalance);
        _balances.Add(otherBalance);

        var result = await CreateSut().RunLeaveYearResetAsync(ResetRequest());

        Assert.That(result.BalancesReset, Is.EqualTo(1));
        AssertReset(ownBalance, 10m);
        Assert.Multiple(() =>
        {
            Assert.That(otherBalance.OpeningBalance, Is.EqualTo(1m));
            Assert.That(otherBalance.AccruedAmount, Is.EqualTo(20m));
            Assert.That(otherBalance.UsedAmount, Is.EqualTo(5m));
            Assert.That(otherBalance.AdjustedAmount, Is.EqualTo(2m));
            Assert.That(otherBalance.CarryForwardAmount, Is.EqualTo(3m));
            Assert.That(otherBalance.BalanceAsOfUtc, Is.EqualTo(otherAsOf));
            Assert.That(otherBalance.UpdatedAtUtc, Is.EqualTo(otherUpdatedAt));
        });
        _balanceRepository.Verify(x => x.Update(ownBalance), Times.Once);
        _balanceRepository.Verify(x => x.Update(otherBalance), Times.Never);
    }

    [TestCase(true, TestName = "U14 latest rule's limit is used (latest listed first)")]
    [TestCase(false, TestName = "U14 latest rule's limit is used (latest listed last)")]
    public async Task RunLeaveYearResetAsync_UsesTheRuleWithTheLatestEffectiveFromDate(bool latestFirst)
    {
        var older = Rule(1, LeaveTypeWithRule, 5m, new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        var latest = Rule(2, LeaveTypeWithRule, 12m, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        _rules.AddRange(latestFirst ? new[] { latest, older } : new[] { older, latest });
        var balance = Balance(1, OrganizationId, LeaveTypeWithRule, 0m, 20m, 5m, 0m, 0m);
        _balances.Add(balance);

        await CreateSut().RunLeaveYearResetAsync(ResetRequest());

        AssertReset(balance, 12m);
    }

    private static void AssertReset(LeaveBalance balance, decimal expectedCarry)
    {
        Assert.Multiple(() =>
        {
            Assert.That(balance.OpeningBalance, Is.EqualTo(0m), "OpeningBalance");
            Assert.That(balance.AccruedAmount, Is.EqualTo(0m), "AccruedAmount");
            Assert.That(balance.UsedAmount, Is.EqualTo(0m), "UsedAmount");
            Assert.That(balance.AdjustedAmount, Is.EqualTo(0m), "AdjustedAmount");
            Assert.That(balance.CarryForwardAmount, Is.EqualTo(expectedCarry), "CarryForwardAmount");
            Assert.That(balance.BalanceAsOfUtc, Is.EqualTo(LeaveYearStart), "BalanceAsOfUtc");
            Assert.That(LeaveMapper.ToResponse(balance).AvailableAmount, Is.EqualTo(expectedCarry), "AvailableAmount");
        });
    }

    private static LeavePolicyRule Rule(int id, int leaveTypeId, decimal? limit, DateTime effectiveFromUtc) => new()
    {
        Id = id,
        OrganizationId = OrganizationId,
        LeaveTypeId = leaveTypeId,
        AccrualRatePerPeriod = 1m,
        MaximumCarryForward = limit,
        IsActive = true,
        EffectiveFromDateUtc = effectiveFromUtc,
    };

    private static LeaveBalance Balance(
        int id,
        int organizationId,
        int leaveTypeId,
        decimal opening,
        decimal accrued,
        decimal used,
        decimal adjusted,
        decimal carry) => new()
    {
        Id = id,
        OrganizationId = organizationId,
        EmployeeId = 100 + id,
        LeaveTypeId = leaveTypeId,
        OpeningBalance = opening,
        AccruedAmount = accrued,
        UsedAmount = used,
        AdjustedAmount = adjusted,
        CarryForwardAmount = carry,
        BalanceAsOfUtc = new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc),
    };
}
