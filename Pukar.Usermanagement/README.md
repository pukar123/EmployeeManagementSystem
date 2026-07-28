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
| `ServiceClients:Ems` | EMS client-credentials (`client_id` + secret) |
| `Cors:AllowedOrigins` | Browser clients |

Generate a dev RSA key:

```bash
dotnet run --project tools/GenRsaKey -- Pukar.Usermanagement.Host/dev-rsa-key.pem
```

## Endpoints

### Public

| Route | Auth |
|-------|------|
| `POST /api/auth/login`, `register`, `refresh`, `revoke` | Anonymous |
| `POST /api/auth/forgot-password`, `reset-password` | Anonymous |
| `POST /api/auth/change-password` | User JWT |
| `GET/POST/PUT /api/users`, `api/roles` | Admin user JWT |
| `POST /api/invitations/accept` | Anonymous |
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
