using EMS.API.IntegrationTests.Harness;
using EMS.Domain.Database;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;

namespace EMS.API.IntegrationTests.Access;

/// <summary>
/// Extra rows for <see cref="LeaveYearResetTests"/> (B11), added on top of <see cref="TestSeed"/> and never changing
/// O1, M1, E1 or E2. Ids are fixed in the 95xx range, which no other seed uses, so cases can name them.
/// <list type="bullet">
/// <item>OR, an organisation of its own, so the reset touches only these rows.</item>
/// <item>LT-Capped, a leave type whose rule allows at most 10 to carry forward, and LT-Uncapped, whose rule has no
/// limit.</item>
/// <item>Year-end balances: R1 on LT-Capped with 15 available, R2 on LT-Capped with −3 available, and R3 on
/// LT-Uncapped with 15 available.</item>
/// </list>
/// </summary>
public static class LeaveYearResetSeed
{
    public const int OrganizationId = 9501;
    public const int CappedLeaveTypeId = 9501;
    public const int UncappedLeaveTypeId = 9502;
    public const int CappedRuleId = 9501;
    public const int UncappedRuleId = 9502;
    public const decimal CarryForwardLimit = 10m;

    public static readonly SeededEmployee R1 = new("R1", EmployeeId: 9511, UserId: 9611, FirstName: "Rae", LastName: "Capped");
    public static readonly SeededEmployee R2 = new("R2", EmployeeId: 9512, UserId: 9612, FirstName: "Rex", LastName: "Negative");
    public static readonly SeededEmployee R3 = new("R3", EmployeeId: 9513, UserId: 9613, FirstName: "Ria", LastName: "Uncapped");

    public const int BalanceR1Capped = 9501;
    public const int BalanceR2Negative = 9502;
    public const int BalanceR3Uncapped = 9503;

    /// <summary>The leave-year start the fixture resets to: 1 July 2027.</summary>
    public static readonly DateTime LeaveYearStart = new(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static Task AddRowsAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;

        db.Organizations.Add(new Organization { Id = OrganizationId, Name = "B11 Year Reset Org", IsActive = true });

        AddLeaveTypeWithRule(db, CappedLeaveTypeId, CappedRuleId, "B11 Capped Leave", CarryForwardLimit, now);
        AddLeaveTypeWithRule(db, UncappedLeaveTypeId, UncappedRuleId, "B11 Uncapped Leave", limit: null, now);

        foreach (var employee in new[] { R1, R2, R3 })
            db.Employees.Add(NewEmployee(employee, now));

        // Year-end amounts: Opening / Accrued / Used / Adjusted / Carry.
        db.LeaveBalances.Add(NewBalance(BalanceR1Capped, R1, CappedLeaveTypeId, accrued: 20m, used: 5m, now));
        db.LeaveBalances.Add(NewBalance(BalanceR2Negative, R2, CappedLeaveTypeId, accrued: 2m, used: 5m, now));
        db.LeaveBalances.Add(NewBalance(BalanceR3Uncapped, R3, UncappedLeaveTypeId, accrued: 20m, used: 5m, now));

        return Task.CompletedTask;
    }

    private static void AddLeaveTypeWithRule(
        AppDbContext db, int leaveTypeId, int ruleId, string name, decimal? limit, DateTime now)
    {
        db.LeaveTypes.Add(new LeaveType
        {
            Id = leaveTypeId,
            OrganizationId = OrganizationId,
            Name = name,
            Unit = LeaveUnit.Days,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            Id = ruleId,
            OrganizationId = OrganizationId,
            LeaveTypeId = leaveTypeId,
            AccrualRatePerPeriod = 1m,
            AccrualFrequency = AccrualFrequency.Monthly,
            MaximumCarryForward = limit,
            IsActive = true,
            EffectiveFromDateUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }

    private static LeaveBalance NewBalance(
        int id, SeededEmployee employee, int leaveTypeId, decimal accrued, decimal used, DateTime now) => new()
    {
        Id = id,
        OrganizationId = OrganizationId,
        EmployeeId = employee.EmployeeId,
        LeaveTypeId = leaveTypeId,
        OpeningBalance = 0m,
        AccruedAmount = accrued,
        UsedAmount = used,
        AdjustedAmount = 0m,
        CarryForwardAmount = 0m,
        BalanceAsOfUtc = new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    private static Employee NewEmployee(SeededEmployee seeded, DateTime now) => new()
    {
        Id = seeded.EmployeeId,
        OrganizationId = OrganizationId,
        DepartmentId = null,
        ManagerId = null,
        EmployeeNumber = $"T-{seeded.Label}",
        FirstName = seeded.FirstName,
        LastName = seeded.LastName,
        Email = $"{seeded.Label.ToLowerInvariant()}.b11@ems-tests.invalid",
        DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DateJoined = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EmploymentStatus = EmploymentStatus.Active,
        IsActive = true,
        IsArchived = false,
        ExternalIdentityKey = seeded.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };
}
