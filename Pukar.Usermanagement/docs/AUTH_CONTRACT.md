# Auth Contract v1

This document defines the stable authentication/authorization contract emitted by `Pukar.Usermanagement` and consumed by EMS.

## Access token claims (required)

- `sub`: user id as string
- `email`: user email
- `jti`: token id
- `authz_contract_version`: `v1`
- role claims:
  - `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` (`ClaimTypes.Role`, canonical display role name)
  - `roles` (normalized uppercase role key for cross-service compatibility)

Role values are trimmed and deduplicated; `roles` values are emitted uppercase.

## Role metadata endpoint

- Route: `GET /api/roles/metadata/v1`
- Authorization: `Admin` role required
- Response shape:
  - `id` (int)
  - `name` (string)
  - `normalizedName` (string, uppercase)
  - `isSystem` (bool)

## Compatibility notes

- Contract version updates must be additive for non-breaking updates.
- Breaking changes require a new contract version and coordinated EMS rollout.
