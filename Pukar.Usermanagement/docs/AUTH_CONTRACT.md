# Auth Contract v2 (standalone host)

Pukar.Usermanagement.Host is the **sole issuer** of user JWTs. EMS and other consumers **validate only** — never sign user tokens.

## User access token

| Setting | Default |
|---------|---------|
| Issuer (`iss`) | `Pukar.Usermanagement` |
| Audience (`aud`) | `ems` |
| Algorithm | RS256 |
| Public keys | `GET /.well-known/jwks.json` |

### Required claims

- `sub` — user id (string)
- `email`
- `jti`
- `authz_contract_version` — `v1`
- `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` — display role names
- `roles` — normalized uppercase role keys

## Service access token (EMS → UM internal APIs)

| Setting | Value |
|---------|-------|
| Issuer | `Pukar.Usermanagement` |
| Audience | `um-internal` |
| Lifetime | 5 minutes (configurable `Jwt:ServiceTokenExpirationMinutes`) |
| Obtain | `POST /api/internal/v1/service-token` with `client_id` + `client_secret` |

### Scopes (`scope` claim)

- `users.read`
- `users.manage`
- `roles.manage`
- `invitations.manage`

### Audit headers (optional, recommended)

- `X-Initiating-User-Id`
- `X-Initiating-User-Email`

Internal endpoints reject unauthenticated callers.

## Role metadata

- Public admin: `GET /api/roles/metadata/v1` (Admin user JWT)
- Internal: `GET /api/internal/v1/roles/metadata` (`roles.manage` scope)

Response shape: `id`, `name`, `normalizedName`, `isSystem`.

## EMS consumer checklist

1. Configure `UserManagementApi:BaseUrl` to the UM host.
2. Validate user JWTs with JWKS (`/.well-known/jwks.json`); do **not** configure `Jwt:SigningKey` for UM tokens.
3. Obtain service tokens for provisioning/sync; include initiating-user headers for audit.
4. Point invitation acceptance UI to UM `POST /api/invitations/accept` (or proxy from EMS).

## Migration from embedded mode (v1 → v2)

| v1 (embedded) | v2 (standalone) |
|---------------|-----------------|
| HS256 shared `Jwt:SigningKey` in EMS | RS256 + JWKS; EMS has no private key |
| Issuer `EMS.API` | Issuer `Pukar.Usermanagement` |
| UM tables in shared DB | `UserManagementDb` database |
| EMS-owned invitations | UM-owned invitations (`ExternalCorrelationId` e.g. `employee:42`) |
| In-process gateway | HTTP internal APIs + service token |

During transition EMS may still embed UM controllers with symmetric validation; production should run **Host only**.

## Compatibility

- Claim contract `v1` is unchanged for role/email/sub semantics.
- Breaking signing/issuer changes require coordinated EMS JWT validation update.
