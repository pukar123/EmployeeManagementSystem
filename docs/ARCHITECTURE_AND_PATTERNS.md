# Architecture, patterns, and methods

Read this document when starting **any** new backend or full-stack project that should follow the same conventions as EMS and Pukar modules. It describes layering, naming, DTOs, services, repositories, shared utilities, authentication module layout, and how repositories relate to each other.

---

## 1. Core principles

| Principle | Rule |
|-----------|------|
| **Layering** | **Domain** → **Application** → **API** (host). **Infrastructure** implements Domain interfaces; Application **must not** reference Infrastructure. |
| **Thin controllers** | HTTP adapters only: validate binding, call **application services**, map **HTTP status** from exceptions. No `DbContext` or raw SQL in controllers. |
| **DTOs at boundaries** | Requests/responses use **flat** models (scalars, enums, FK ids). No EF navigation properties on DTOs. |
| **One bounded context per area** | Folders and namespaces group by business area (`Employee`, `Organization`, `Department`, …). |
| **Shared cross-cutting code** | Generic string helpers and business-rule exceptions live in **`Pukar.Shared`** (namespace `Pukar.Shared`), referenced by Domain/Application/API as needed—not duplicated per project. |

---

## 2. Standard solution layout (backend)

Use this shape unless a project is intentionally smaller:

| Layer | Responsibility | Typical name pattern |
|-------|------------------|----------------------|
| **Domain** | Entities (`DbModels/`), EF `DbContext`, **Fluent API configurations**, **migrations**, repository **interfaces** (`IBaseRepository<T>`, feature-specific repos if needed). | `{Product}.Domain` |
| **Application** | **DTOs** per area (`DTOs/{Area}/`), **application services** (`Services/{Area}/`), **mapping** (`Mapping/` or area-local mappers). | `{Product}.Application` |
| **Infrastructure** | **Repository implementations** (`BaseRepository<T>`), EF wiring, external adapters (e.g. file storage helpers). | `{Product}.Infrastructure` |
| **API** | ASP.NET Core **host**: controllers, `Program.cs`, middleware, DI registration, health, OpenAPI. | `{Product}.API` |

**Dependency direction**

```text
API  →  Application  →  Domain (interfaces, entities)
         ↑
Infrastructure  →  Domain (implements interfaces; uses DbContext)
```

- **Application** references **Domain** only (not Infrastructure).
- **Infrastructure** references **Domain** + **Application** is *not* required; it implements interfaces declared in Domain.
- **API** references **Application**, **Infrastructure** (for DI), and **Domain** only if needed for hosting (e.g. migrations tooling).

---

## 3. Naming conventions

### Projects and assemblies

- **PascalCase** with a clear **prefix** per product: `EMS.*`, `Pukar.Usermanagement.*`, `Pukar.Shared`.
- **Solution file** at repo root: e.g. `EmployeeManagementSystem.sln`; optional second solution for a standalone module (e.g. `Pukar.Usermanagement/Pukar.Usermanagement.sln`).

### Types and members

- **Public** types / members: **PascalCase** (`DepartmentService`, `GetByIdAsync`).
- **Interfaces**: prefix **`I`** (`IDepartmentService`, `IBaseRepository<T>`).
- **Async** I/O methods: suffix **`Async`**, return `Task` / `Task<T>`.
- **Parameters and locals**: **camelCase** (`cancellationToken`, `organizationId`).
- **Private fields** (if used): `_camelCase` (match existing code).

### DTOs (Application layer)

| Shape | Name pattern |
|-------|----------------|
| Create body | `Create{Entity}RequestModel` |
| Update body | `Update{Entity}RequestModel` |
| API read model | `{Entity}ResponseModel` |
| Composite transfer inside app | `{Entity}DTO` when useful |

- Namespaces: `EMS.Application.DTOs.{Area}` (or `{Product}.Application.DTOs.{Area}`).
- **Flat** shapes only; foreign keys as `int` / `int?`.

### Controllers (API)

- Class: **`{PluralResource}Controller`** (`EmployeesController`, `DepartmentsController`).
- Route: `[Route("api/[controller]")]` unless versioning dictates otherwise.
- Actions: names that reflect HTTP semantics (`GetAll`, `GetById`, `Create`, `Update`, `Delete`).

### Files

- **One primary public type per file**; **file name** matches the type name.
- Mapping: `internal static` mapper classes under `Mapping/` or next to the service they serve.

---

## 4. Application services

- Interface: **`I{Entity}Service`** in `Application/Services/{Area}/`.
- Implementation: **`{Entity}Service`** in the same folder.
- Orchestration: load/save via **`IBaseRepository<T>`** (or specialized repository), apply **business rules**, throw **`BusinessRuleException`** (`Pukar.Shared`) for rule violations.
- Normalize strings with **`StringHelper`** (`Pukar.Shared`) where appropriate (`NormalizeOptional`, `NormalizeRequired`, email validation).

---

## 5. Repositories

- **Interface** `IBaseRepository<T>` in **Domain** (generic CRUD + `GetQueryable()` + transactions as needed).
- **Implementation** `BaseRepository<T>` in **Infrastructure**, bound to `DbContext`.
- **Async methods** on `IBaseRepository<T>` take **`CancellationToken cancellationToken = default`**; implementations forward it to EF Core. Application services should **pass through** the token from controller actions so request cancellation reaches the database layer.
- Register in **API** `Program.cs`: `AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>))` (or equivalent).

---

## 6. Exceptions and shared helpers (`Pukar.Shared`)

| Type | Use |
|------|-----|
| **`BusinessRuleException`** | Expected business failures (duplicate name, invalid state). Controllers catch and map to **400/409** with a safe message. |
| **`DuplicateEmailException`** | Subclass for auth registration conflicts (optional). |
| **`EmailNormalizer`** | Upper-invariant email for lookups. |
| **`StringHelper`** | Trim/normalize, optional vs required strings, email format check, comparisons. |

**Do not** duplicate these in each Domain; **reference `Pukar.Shared`** from Domain/Application/API where needed.

---

## 7. EMS-specific layout (this repository)

| Project | Role |
|---------|------|
| `EMS.Domain` | `AppDbContext`, `DbModels/`, `Configurations/`, migrations, `IBaseRepository<T>` |
| `EMS.Application` | DTOs, services, mapping |
| `EMS.Infrastructure` | `BaseRepository<T>` |
| `EMS.API` | Controllers, `Program.cs`, CORS, Serilog, health checks; **JWT validation only** (does not issue tokens) |
| `EMS.ArchitectureTests` | NetArchTest rules: EMS must not reference UM implementation assemblies |
| `ems-web` | Next.js App Router client; `NEXT_PUBLIC_EMS_API_BASE_URL` (EMS), `NEXT_PUBLIC_USER_MANAGEMENT_API_BASE_URL` (User Management Host) |

**Migrations:** `EMS.Domain`; startup project for EF tools: **`EMS.API`**.

**EMS ↔ User Management boundary:** EMS consumes User Management **exclusively through HTTP contracts** (`Pukar.Usermanagement.Contracts`). EMS never references `Pukar.Usermanagement.API`, `.Application`, `.Domain`, or `.Infrastructure`, and never reads or writes User Management tables. See [ems-um-http-integration.md](ems-um-http-integration.md).

More diagrams: [architecture.md](architecture.md) (EMS-focused overview).

---

## 8. Pukar.Usermanagement module (standalone identity service)

| Project | Role |
|---------|------|
| `Pukar.Shared` | Shared helpers/exceptions (also used by EMS) — lives under `Pukar.Usermanagement/Pukar.Shared/` |
| `Pukar.Usermanagement.Contracts` | HTTP DTOs, service scopes, well-known roles — **only UM assembly EMS may reference** |
| `Pukar.Usermanagement.Domain` | `User`, `Role`, `AccountInvitation`, `UserManagementDbContext`, schema **`um`**, migrations |
| `Pukar.Usermanagement.Application` | Auth, invitations, internal services |
| `Pukar.Usermanagement.Infrastructure` | Repositories, JWT signing (RSA), BCrypt, SMTP |
| `Pukar.Usermanagement.API` | Controllers (public + `api/internal/v1/*`) |
| `Pukar.Usermanagement.Host` | **Canonical deployment host** — issues JWTs, owns identity data |

**EMS integration (HTTP only):**

- EMS validates user JWTs via UM issuer/audience/JWKS (`AddUserManagementJwtAuthentication`).
- EMS calls internal APIs with a cached **service token** (`POST /api/internal/v1/service-token`).
- Anti-corruption interface: `IEmployeeUserManagementGateway` → `HttpEmployeeUserManagementGateway`.
- Invitations: EMS orchestrates eligibility and stores `Employee.ExternalIdentityKey`; passwords, tokens, and email delivery stay in UM.
- Position/employee role assignments store stable **`RoleKey`** (normalized name), not numeric UM role ids.
- EMS-owned: `RoleKeyPermission`, `RoleKeyCapability`, menus, integration outbox.

Standalone clone: build **`Pukar.Usermanagement.sln`** (includes `Pukar.Shared`). See [Pukar.Usermanagement/README.md](../Pukar.Usermanagement/README.md).

---

## 9. Frontend (`ems-web`)

- **Next.js** App Router, feature folders under `src/features/{area}/` (`components`, `hooks`, `services`, `types`).
- **HTTP:** `emsHttpClient` → EMS (`NEXT_PUBLIC_EMS_API_BASE_URL`); `userManagementHttpClient` → User Management Host (`NEXT_PUBLIC_USER_MANAGEMENT_API_BASE_URL`) for auth, invitation accept, and admin users/roles.
- Local stack: `npm run dev:all` starts UM Host, EMS.API, and Next.js.
- See [ems-web/README.md](../ems-web/README.md) and [ems-um-http-integration.md](ems-um-http-integration.md).

---

## 10. Git and distribution

| Topic | Practice |
|-------|----------|
| **Standalone module** | `git subtree split -P Pukar.Usermanagement` pushes self-contained history to a `Pukar.Usermanagement` remote when you maintain it separately. |
| **Submodule** | Optional: replace in-tree folder with `git submodule add` after remote exists; see [SUBMODULE_SETUP.md](../SUBMODULE_SETUP.md). |
| **NuGet** | Pack metadata in `Directory.Build.props`; publish when consumers should use `PackageReference` instead of project references. |

---

## 11. Checklist for a new bounded context (area)

1. Add **entities** + EF configuration + migration in **Domain**.
2. Add **DTOs** in **Application** (`Create`/`Update`/`Response`).
3. Add **service interface + implementation**; use **`IBaseRepository<T>`** and **`BusinessRuleException`** / **`StringHelper`** as needed.
4. Add **mapper** if mapping is non-trivial.
5. Register service in **API** `Program.cs`.
6. Add **controller** with try/catch for **`BusinessRuleException`** mapping to appropriate HTTP responses.
7. Add **tests** (when a test project exists) at Application or API level.

---

## 12. Cursor / IDE rules

- Layering and naming rules are also in [`.cursor/rules/`](../.cursor/rules/) (e.g. `ems-architecture-layers.mdc`, `ems-csharp-conventions.mdc`, `ems-web-conventions.mdc`).
- Prefer **this document** as the human-readable overview; **Cursor rules** as machine-scoped hints.

---

## 13. Recent implementation notes

### Navigation and menu permissions

- Sidebar navigation now defensively deduplicates effective menu entries before rendering (same parent + normalized route + label) to prevent duplicate items such as `Menu access`.
- Navigation tree construction in `EMS.Application` includes a backend dedupe safeguard for effective route/parent collisions before serializing menu children.
- Menu permission management in `ems-web` now uses a collapsible tree (instead of a flat table) with:
  - parent-child hierarchy by `parentMenuId`
  - tri-state checkboxes (checked/unchecked/indeterminate)
  - branch actions: `Select all` and `Clear all`

### Seeding hardening

- EMS RBAC seed logic now checks both menu key and logical parent+route match to reduce duplicate logical rows during repeated startup seeding.

### Employee linked-user temporary password

- Employee linked-user provisioning now generates temporary passwords as `FirstName@123`.
- If first name cannot produce a valid segment, fallback format is `EMP{EmployeeNumber}@123`.
- Newly provisioned users still have `MustChangePassword = true`.

### Historical tracking and retention

- Historical tracking now follows a cross-cutting architecture:
  - automatic audit rows are captured through EF save interception (no per-controller/per-endpoint manual audit code)
  - effective-dated employee history is persisted for position, department, and manager changes
  - employee delete requests are retention-safe archives (soft delete), not hard deletes
- New domain artifacts for this pattern live in `EMS.Domain`:
  - `AuditTrailEntry`
  - `EmployeePositionHistory`
  - `EmployeeDepartmentHistory`
  - `EmployeeManagerHistory`
  - `EmployeeRetentionPolicy`
- Application services remain the orchestration layer:
  - `EmployeeService` detects transition deltas and writes history events
  - retention deadline is derived from `EmployeeRetentionPolicy` and stored on employee archive metadata
- API read access for timelines is exposed separately via `EmployeeHistoryController`.

### Attendance reporting and analytics

- Attendance reporting and analytics follow the same layered path as operational attendance actions:
  - `AttendanceController` exposes report, analytics, and export endpoints only.
  - `IAttendanceService` and `AttendanceService` own report aggregation logic and policy-aware analytics calculations.
  - `IAttendanceRepository` defines report query contracts; `AttendanceRepository` implements EF Core query details in Infrastructure.
- Reporting DTOs are flat and live under `EMS.Application/DTOs/Attendance/`:
  - `AttendanceReportFilterRequestModel`
  - `AttendanceDailySummaryResponseModel`
  - `AttendancePeriodSummaryResponseModel`
  - `AttendancePunctualityResponseModel`
  - `AttendanceAbsenteeismResponseModel`
  - `AttendanceExportRequestModel`
  - `AttendanceExportFileResponseModel`
- Export support is delivered in three formats: CSV, Excel (`.xlsx`), and PDF, all generated by Application service orchestration and streamed by the API controller.
- Frontend attendance reporting remains feature-scoped in `ems-web/src/features/attendance` with clear separation:
  - `services`: API access through shared `http-client`
  - `hooks`: query/mutation orchestration
  - `components`: report and analytics rendering
  - `types`: report/analytics contracts aligned with backend DTOs

### Local build: MSB3027 / MSB3021 (file locked)

If **`dotnet build`**, **`dotnet ef`**, or Visual Studio reports errors copying DLLs into **`EMS.API\bin\Debug\...`** (often after many **MSB3026** retries), the usual cause is that **EMS.API is already running** and has those assemblies loaded.

**Fix:** stop the running API (debug session, `dotnet run` terminal, or Task Manager process **EMS.API**), then build again. This is not a C# compiler failure.

---

*Keep this file updated when you add new cross-cutting patterns (validation, CQRS, MediatR, etc.) so future projects stay consistent.*
