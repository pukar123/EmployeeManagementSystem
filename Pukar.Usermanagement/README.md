# Pukar.Usermanagement

JWT authentication, user/role administration, invitations, password reset, and service APIs.

> **Deployment note:** these projects (`Contracts`, `Domain`, `Application`, `Infrastructure`, `API`) are now **composed in-process by `EMS.API`**, which is the single deployable host and composition root. User Management keeps its own `UserManagementDbContext`, `UserManagementDb`, and EF migrations. `Pukar.Usermanagement.Host` is retained for reference/tests but is **no longer required at runtime** — do not deploy it as a separate service. EMS registers everything through `AddPukarUserManagement(configuration, "UserManagementDb", useRsaSigning: false)` and serves the UM controllers via an MVC application part.

## Projects

| Project | Role |
|---------|------|
| `Pukar.Shared` | Shared helpers/exceptions (also used by EMS) |
| `Pukar.Usermanagement.Contracts` | Stable public API contracts, scopes, claim constants |
| `Pukar.Usermanagement.Domain` | Entities, `UserManagementDbContext`, migrations |
| `Pukar.Usermanagement.Application` | Services, options |
| `Pukar.Usermanagement.Infrastructure` | EF repositories, JWT (RS256), SMTP, DI |
| `Pukar.Usermanagement.API` | Controllers and auth extensions |
| `Pukar.Usermanagement.Host` | **Executable host** (canonical deployment) |

## Standalone startup

```bash
dotnet run --project Pukar.Usermanagement.Host
```

Default URLs: `https://localhost:7098` / `http://localhost:5137` (see `launchSettings.json`).

## Database

Use a **dedicated** SQL Server database (`UserManagementDb`), separate from EMS `AppDbContext`:

```json
"ConnectionStrings": {
  "UserManagementDb": "Server=...;Database=UserManagementDb;..."
}
```

Apply migrations (Host is the startup project):

```bash
dotnet ef database update --project Pukar.Usermanagement.Domain --startup-project Pukar.Usermanagement.Host --context UserManagementDbContext
```

If you previously stored `um` schema inside `EMSDevDB`, use the idempotent split migrator before cutover:

```bash
dotnet run --project tools/UmDbSplitMigrator -- --mode dry-run --source "<EMS>" --target "<UserManagementDb>"
dotnet run --project tools/UmDbSplitMigrator -- --mode apply --source "<EMS>" --target "<UserManagementDb>"
dotnet run --project tools/UmDbSplitMigrator -- --mode validate --source "<EMS>" --target "<UserManagementDb>"
```

Full cutover sequence: [docs/ems-um-db-split-cutover.md](../docs/ems-um-db-split-cutover.md).

## Configuration

| Section | Purpose |
|---------|---------|
| `ConnectionStrings:UserManagementDb` | SQL Server database |
| `Jwt` | Issuer (`Pukar.Usermanagement`), audience (`ems`), RSA key (`SigningKeyPem` or `SigningKeyPemFile`) |
| `SeedAdmin` | Optional default admin user on startup |
| `Smtp` | Invitation and password-reset email delivery; use Gmail SMTP with an app password for local development |
| `PasswordReset` | Password-reset token lifetime and request cooldown |
| `Registration` | Public self-registration switch (off by default), see below |
| `AuthRateLimiting` | Limits for the anonymous sign-in and password-recovery endpoints, see below |
| `ServiceClients:Ems` | EMS client-credentials (`client_id` + secret) |
| `Cors:AllowedOrigins` | Browser clients |

Generate a dev RSA key:

```bash
dotnet run --project tools/GenRsaKey -- Pukar.Usermanagement.Host/dev-rsa-key.pem
```

## Sign-in and registration protection

### Self-registration

`Registration:AllowSelfRegistration` (default `false`). When false, `POST /api/auth/register` returns **404** with `{ "code": "registration_disabled", "message": "Self-registration is not available. Ask your administrator for an invitation." }` for any body, creates nothing and issues no tokens. The check is the first thing `AuthService.RegisterAsync` does, so the answer is the same whether or not the email exists. Set it to `true` only for a standalone deployment that really wants open sign-up. In EMS, accounts come from employee invitations and the `SeedAdmin` account.

### Rate limiting

Both hosts (EMS.API in-process and `Pukar.Usermanagement.Host`) call `AddPukarUserManagementRateLimiting(configuration)` and `UsePukarUserManagementRateLimiting()` (after `UseCors`, before `UseAuthentication`). Limits are fixed windows held in memory per process (counters reset on restart; a multi-instance deployment would need a distributed limiter). Only the endpoints below are limited; there is no global limiter, and CORS preflight requests are not counted.

| Setting | Default | Applies to |
|---------|---------|------------|
| `AuthRateLimiting:Enabled` | `true` | Master switch. `false` turns every limit below off (local troubleshooting only). |
| `AuthRateLimiting:SignInPerIp:PermitLimit` / `:WindowSeconds` | `20` / `60` | `POST /api/auth/login`, per client IP |
| `AuthRateLimiting:SignInPerEmail:PermitLimit` / `:WindowSeconds` | `5` / `300` | `POST /api/auth/login`, per normalised email across all IPs (every attempt counts) |
| `AuthRateLimiting:RegisterPerIp:PermitLimit` / `:WindowSeconds` | `5` / `3600` | `POST /api/auth/register`, per client IP |
| `AuthRateLimiting:RefreshPerIp:PermitLimit` / `:WindowSeconds` | `60` / `60` | `POST /api/auth/refresh`, per client IP |
| `AuthRateLimiting:ForgotPasswordPerIp:PermitLimit` / `:WindowSeconds` | `10` / `900` | `POST /api/auth/forgot-password`, per client IP |
| `AuthRateLimiting:ForgotPasswordPerEmail:PermitLimit` / `:WindowSeconds` | `3` / `3600` | `POST /api/auth/forgot-password`, per normalised email across all IPs (on top of the `PasswordReset:RequestCooldownSeconds` cooldown) |
| `AuthRateLimiting:ResetPasswordPerIp:PermitLimit` / `:WindowSeconds` | `10` / `900` | `POST /api/auth/reset-password` and `POST /api/invitations/accept`, per client IP (separate counters, same settings) |

Every limit and window must be at least 1; anything else stops the host at startup with a message naming the setting.

When a limit is reached the API answers **429** with a whole-second `Retry-After` header and `{ "code": "rate_limited", "message": "Too many attempts. Please wait a moment and try again.", "retryAfterSeconds": <same number> }`. The password is not checked and the database is not touched. The answer is the same whether or not the account exists. CORS policies expose `Retry-After` so the web app can read it; the body carries the same number in case a proxy drops the header. Rejections are logged as warnings with the policy name and path only, never the email or password.

**Client IP.** The per-IP key is the connection's remote address (`"unknown"` when the host gives none). `X-Forwarded-For` and similar headers are **not** trusted: without a known reverse proxy they are chosen by the caller and would let anyone bypass the limit. Behind a shared IP (office NAT, Docker Desktop port forwarding, the Next.js dev rewrite proxy) many users count as one client, so the per-IP defaults are generous and the per-email limits do the account-specific work. Adding a reverse proxy needs `UseForwardedHeaders` with explicit `KnownProxies` first (a later task). Account lockout after failed attempts is a separate later task (B13).

## Endpoints

### Public

| Route | Auth |
|-------|------|
| `POST /api/auth/login`, `refresh`, `revoke` | Anonymous (login and refresh rate limited) |
| `POST /api/auth/register` | Anonymous, **off by default** (404 `registration_disabled`); rate limited when on |
| `POST /api/auth/forgot-password`, `reset-password` | Anonymous, rate limited |
| `POST /api/auth/change-password` | User JWT |
| `GET/POST/PUT /api/users`, `api/roles` | Admin user JWT |
| `POST /api/invitations/accept` | Anonymous, rate limited |
| `GET /.well-known/jwks.json` | Anonymous |
| `GET /health` | Anonymous |

### Internal (service JWT required)

| Route | Scope |
|-------|-------|
| `POST /api/internal/v1/service-token` | Client credentials (anonymous) |
| `POST /api/internal/v1/users/lookup` | `users.read` |
| `GET /api/internal/v1/users/by-email` | `users.read` |
| `POST /api/internal/v1/users/{id}/activate` | `users.manage` |
| `POST /api/internal/v1/users/{id}/deactivate` | `users.manage` |
| `POST /api/internal/v1/users/{id}/revoke-sessions` | `users.manage` |
| `PUT /api/internal/v1/users/{id}/roles` | `roles.manage` |
| `GET /api/internal/v1/roles/metadata` | `roles.manage` |
| `POST/GET/DELETE /api/internal/v1/invitations` | `invitations.manage` |

Forward audit context from EMS using headers `X-Initiating-User-Id` and `X-Initiating-User-Email`.

## Service scopes

- `users.read` — batch lookup, lookup by email
- `users.manage` — activate/deactivate, revoke sessions
- `roles.manage` — metadata, replace roles by normalized keys
- `invitations.manage` — create/list/resend/revoke invitations

## EMS integration (next phase)

EMS should validate user JWTs via `/.well-known/jwks.json` only (no private key). Call internal APIs with an EMS service token from `POST /api/internal/v1/service-token`.

Claim contract: [docs/AUTH_CONTRACT.md](docs/AUTH_CONTRACT.md).

## Build & test

```bash
dotnet build Pukar.Usermanagement.sln
dotnet test Pukar.Usermanagement.sln
```
