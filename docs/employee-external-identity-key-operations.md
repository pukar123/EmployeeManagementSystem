# Employee `ExternalIdentityKey` (identity user link)

EMS enforces **at most one non-archived employee per identity user id** stored in `org.Employees.ExternalIdentityKey` (filtered unique index + application checks on provision and role sync).

## Before applying the `ActiveEmployeeExternalIdentityKeyUnique` migration

Creating the filtered unique index **fails** if two or more **active** rows share the same non-null `ExternalIdentityKey`. Clean duplicates first.

Run on the target SQL Server database:

```sql
-- Duplicate active keys (should return no rows before migration)
SELECT ExternalIdentityKey, COUNT(*) AS ActiveEmployeeCount
FROM org.Employees
WHERE ExternalIdentityKey IS NOT NULL
  AND IsArchived = 0
GROUP BY ExternalIdentityKey
HAVING COUNT(*) > 1;
```

Optional detail for remediation (which employees conflict):

```sql
SELECT e.Id,
       e.OrganizationId,
       e.EmployeeNumber,
       e.Email,
       e.ExternalIdentityKey,
       e.IsArchived
FROM org.Employees AS e
WHERE e.ExternalIdentityKey IS NOT NULL
  AND e.IsArchived = 0
  AND e.ExternalIdentityKey IN (
      SELECT ExternalIdentityKey
      FROM org.Employees
      WHERE ExternalIdentityKey IS NOT NULL
        AND IsArchived = 0
      GROUP BY ExternalIdentityKey
      HAVING COUNT(*) > 1
  )
ORDER BY e.ExternalIdentityKey, e.Id;
```

Resolve by clearing the key on the wrong row, archiving obsolete rows, or merging records as appropriate for your org. Then apply the migration (for example `dotnet ef database update` from your usual host project).
