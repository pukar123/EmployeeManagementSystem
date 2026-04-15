# Authorization Cutover Checklist

Use this checklist before switching fully to role-key authorization.

## Endpoints for readiness checks

- Telemetry snapshot: `GET /api/authorizationtelemetry`
- Cutover readiness snapshot: `GET /api/authorization-cutover/readiness`

Both endpoints require an Admin token.

## Required conditions before cutover

1. `UseRoleKeyMapping` is enabled.
2. `EnableLegacyRoleIdFallback` remains enabled during parity soak period.
3. `MismatchRatePercent` stays at or below threshold (`1.0%`) across the agreed window.
4. `LegacyFallbackUsedTotal` trends down and does not increase unexpectedly after backfill.
5. No critical authorization regressions reported in staging smoke/regression tests.

## Flag sequence

1. Start with:
   - `UseRoleKeyMapping=false`
   - `EnableLegacyRoleIdFallback=true`
2. Enable role-key path:
   - `UseRoleKeyMapping=true`
   - `EnableLegacyRoleIdFallback=true`
3. After stable parity window:
   - `UseRoleKeyMapping=true`
   - `EnableLegacyRoleIdFallback=false`

## Go/No-Go guidance

- **Go** when readiness endpoint reports:
  - `MeetsMismatchThreshold=true`
  - fallback usage is acceptable for your environment
  - operational sign-off is complete
- **No-Go** when mismatch exceeds threshold or fallback spikes unexpectedly.

## Rollback

If issues appear after changing flags:

1. Immediately set:
   - `UseRoleKeyMapping=false`
   - `EnableLegacyRoleIdFallback=true`
2. Investigate mismatch telemetry and recent permission data changes.
3. Re-run parity soak before retrying cutover.
