# Leave-year reset correction (B11)

This note explains how to find leave balances that a leave-year reset doubled before the B11 fix, and how to correct them by hand. EMS is a personal portfolio project; the steps are written as they would be run against a real SQL Server database.

**B11 changes no data automatically.** There is no migration and no start-up fix. Balances a pre-fix reset doubled stay as they are until someone reviews and corrects them with the steps below.

Run the check **before the next leave-year reset**. If the reset runs again first, the doubled amount is rolled into the new carry as if it were real leave (capped only by any carry-forward limit), and the fingerprint used below is lost.

## What happened

The available balance is worked out as:

```
Available = OpeningBalance + AccruedAmount + AdjustedAmount + CarryForwardAmount - UsedAmount
```

Before B11, the leave-year reset (`POST /api/LeaveOperations/year-reset/run`) wrote the carried amount into **both** `OpeningBalance` and `CarryForwardAmount`, so the new year started at twice the carry. For example, 15 left at year end with a limit of 10 showed 20 instead of 10. Every reset run compounded it: with no limit, a balance doubled on each run.

Since B11 the reset sets `OpeningBalance` to 0 and keeps the carry only in `CarryForwardAmount`, so the new year starts at exactly the carry.

## 1. Find likely doubled balances (read-only)

```sql
SELECT lb.Id, lb.OrganizationId, lb.EmployeeId, lb.LeaveTypeId,
       lb.OpeningBalance, lb.AccruedAmount, lb.UsedAmount, lb.AdjustedAmount, lb.CarryForwardAmount,
       lb.BalanceAsOfUtc, lb.UpdatedAtUtc,
       lb.OpeningBalance + lb.AccruedAmount + lb.AdjustedAmount + lb.CarryForwardAmount - lb.UsedAmount AS AvailableNow,
       lb.AccruedAmount + lb.AdjustedAmount + lb.CarryForwardAmount - lb.UsedAmount                     AS AvailableIfCorrected
FROM org.LeaveBalances AS lb
WHERE lb.CarryForwardAmount > 0
  AND lb.OpeningBalance = lb.CarryForwardAmount
ORDER BY lb.OrganizationId, lb.EmployeeId, lb.LeaveTypeId;
```

Why this matches: nothing else in EMS sets `OpeningBalance` (new balances start at 0), so `OpeningBalance = CarryForwardAmount > 0` is the pre-fix reset's fingerprint. After B11, resets leave `OpeningBalance` at 0, so new rows never match.

## 2. Check each row by hand before correcting

- **Rows reset more than once before the fix.** The carry itself may be inflated (with no limit it doubled on each run). Setting `OpeningBalance` to 0 removes only the latest doubling. Rebuild the right carry from HR records or the approved leave history (there is no leave ledger yet; that is B17). Hints that a row was reset more than once: a carry far above what one year's accrual could give, or several known reset runs.
- **Rows where `AvailableIfCorrected` is below 0.** The employee has already taken some of the phantom leave. Decide case by case, for example with an `AdjustedAmount` entry recorded with a reason. Do not silently push the balance negative.
- **Imported or hand-entered opening balances** that happen to equal the carry also match the query. Exclude them after review.

## 3. Correct the reviewed rows

Take a database backup first. Then, for the reviewed ids only:

```sql
BEGIN TRANSACTION;
UPDATE org.LeaveBalances
SET OpeningBalance = 0, UpdatedAtUtc = SYSUTCDATETIME()
WHERE Id IN (/* reviewed ids */)
  AND OpeningBalance = CarryForwardAmount;   -- safety: only rows still in the doubled shape
-- re-run the SELECT above for these ids and check AvailableIfCorrected, then:
COMMIT;  -- or ROLLBACK;
```

After the update, the balance endpoint's available amount for each corrected row equals the old `AvailableIfCorrected`.

## Limits of this note

- The detection query is a heuristic: it cannot recover the true value of a row doubled more than once, and it can match a legitimate opening balance. Review every row.
- The SQL has not been run by the automated tests (they use EF Core InMemory). Try it on a copy of the database first.
- Running the reset again for the same year after new-year accrual or leave still rolls that activity into the carry. A run guard is planned separately (B12).
