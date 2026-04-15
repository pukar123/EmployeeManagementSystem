# Role-Key Permission Backfill

This runbook migrates legacy role-id permissions in `ems.RolePermissions` into the new role-key model `ems.RoleKeyPermissions`.

## Why this exists

EMS is moving from direct coupling to `um.Roles.Id` toward role-key authorization using normalized role keys (`um.Roles.NormalizedName`). During migration, both models coexist.

## Preconditions

1. Apply latest EMS migrations (must include `AddRoleKeyPermissions`).
2. `um.Roles` and `ems.RolePermissions` are available in the same SQL database.
3. Take a database backup/snapshot before running in shared environments.

## Script

Run:

`scripts/backfill-role-key-permissions.sql`

What it does:

- Reads source rows from `ems.RolePermissions`.
- Resolves `RoleKey` from `um.Roles.NormalizedName`.
- Upserts into `ems.RoleKeyPermissions`.
- Is idempotent (safe to re-run).

## Post-run validation

The script prints:

- total row counts for legacy and role-key tables
- per-role row counts for both models

Expected outcome:

- `RoleKeyPermissionRows` should be greater than zero for seeded/active systems.
- per-role counts should generally match legacy counts, except where legacy data had missing or invalid role references.

## Known edge cases

- Legacy rows with missing `um.Roles` references are skipped (no valid `RoleKey`).
- If `Allowed` values differ on rerun, target rows are updated to match legacy source.

## Rollback

If needed, restore from backup/snapshot.

For partial rollback without restore:

1. Stop rollout flags (`UseRoleKeyMapping=false`).
2. Optionally clear target table:
   `DELETE FROM ems.RoleKeyPermissions;`

Re-run script after correcting data issues.
