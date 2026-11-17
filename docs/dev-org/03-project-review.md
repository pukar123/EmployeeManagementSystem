# 03 — Project review: EMS (Employee Management System)

**Reviewer:** Project Reviewer (AI team)  **Date:** 15 November 2026  **Scope:** whole repository, read-only
**Working copy reviewed:** `/home/claude/work/ems` at HEAD `e053772` ("impl"). That commit holds the manager leave-approval feature from run `20261115-2052` (34 files, never compiled). EMS is a personal portfolio project, not commercial work.

**What I actually ran**
- Static reading of the backend, User Management (UM), the docs, Docker and the `.cursor/rules/*.mdc` standards.
- In a copy (`/tmp/ems-web-review`), I ran `npm ci`, `npx eslint .`, `npx tsc --noEmit`, `npx vitest run`, `npx next build` and `npm audit --omit=dev`. Results: lint 0 errors and 7 warnings; tsc clean; 35/35 tests passed in 9 files; production build succeeded; audit found 33 vulnerabilities.
- I did **not** run `dotnet build` or `dotnet test`, because there is no .NET SDK or NuGet access here. No backend test results are claimed.

---

## Executive summary

EMS covers more ground than most portfolio projects. It has the employee lifecycle (scheduled changes, transfers, history), leave with balances, accrual and approval, attendance with PDF and Excel export, onboarding, tasks, shifts, notifications, an audit trail, an integration outbox, and a separate in-process User Management bounded context with properly hashed, rotating refresh tokens. The layering is mostly clean, the frontend builds, lints and type-checks cleanly, and the docs on migrations and operations are better than average.

The biggest problem is **authorisation**. About half the API controllers check only that the caller is logged in, not what they are allowed to do. The API also lets anyone self-register anonymously (`/api/auth/register`) and get a valid token. Together, this means anyone who can reach the API can:
- read and download every employee's documents
- give the ADMIN role to any job position
- change the organisation, departments, sites and menus
- run leave accrual or year reset (accrual can be run repeatedly to inflate balances)

On top of that, uploaded employee documents and leave attachments sit in the public web root and are served **without any login at all**.

A technical reviewer looking at an HR system will check these things first. They should be fixed before EMS is shown to employers.

The second theme is **evidence of quality**:
- The backend has unit tests for employees, leave and manager areas only.
- The "integration test" is a placeholder (`Assert.That(true, Is.True)`).
- There is no CI.
- At least one architecture test would fail if it were run (C8).
- The README still says "Automated tests are not included yet".
- The Docker Compose API image cannot build as written.

**Overall:** strong functional breadth and good instincts (audit, outbox, gated migrations), but it is not yet safe or verifiable enough to present as "production-style". About one to two focused weeks on C1 to C9 would change how it reads to a hiring manager.

Counts: **4 Critical, 6 High, 8 Medium, 5 Low.**

---

## Strengths

1. **Clear layered solution, with boundaries enforced by tests.**
   - The projects are Domain, Application, Infrastructure and API.
   - `EMS.ArchitectureTests/UserManagementBoundaryTests.cs` uses NetArchTest to stop inner layers referencing User Management implementation projects, and Application referencing Infrastructure.
2. **Real business breadth.**
   - Employee lifecycle: archive, status history, scheduled changes with a background worker, and transfers with department, position and manager history tables.
   - Leave: types, policy rules, balances, accrual, year reset, bulk import, attachments, and the new manager approval.
   - Also attendance (breaks, punctuality and absenteeism analytics, PDF/XLSX export via QuestPDF/ClosedXML), onboarding checklists, tasks, shifts and an in-app notification centre.
3. **Audit trail built into persistence.** `EMS.Infrastructure/Persistence/Auditing/AuditSaveChangesInterceptor.cs` records old and new values as JSON, along with the actor and correlation ID, for every add, change and delete.
4. **Reliable cross-database integration.** Creating an employee writes a `ProvisionEmployeeIdentity` message to an EMS outbox. `IntegrationOutboxDispatcher` then processes it with idempotency, so there is no distributed transaction and no message broker to run.
5. **Sound identity basics in User Management.**
   - Passwords are hashed with BCrypt.
   - Refresh tokens are stored hashed and rotated on refresh (`AuthService.cs:118-128`).
   - Access tokens last 15 minutes, and there is a configurable password policy.
   - Every controller requires authentication by default through a global `AuthorizeFilter` (`Program.cs`).
   - CORS refuses to start outside Development without configured origins.
6. **Careful migration operations.**
   - The destructive `RemoveLegacyUserManagementTables` migration only runs once an operator creates a marker table.
   - Architecture tests check the migration source.
   - There are runbooks for the database split, role-key backfill and authorisation cutover.
7. **Capability-based access in the Employees area.** `EmployeeAccessService` (view, manage, export, own profile) and `ManagerTeamAccessService` tie managers to their linked employee record and organisation, and both have unit tests.
8. **Optimistic concurrency where it matters most.** `LeaveRequest` and `EmployeeScheduledChange` have `RowVersion`, and approval handles `DbUpdateConcurrencyException`.
9. **Modern, healthy frontend.**
   - Next.js 16 App Router, organised by feature (`src/features/{area}/components|hooks|services|types`).
   - React Query for server state, Zustand for UI state only, react-hook-form with zod, and a refresh-token interceptor with single-flight refresh.
   - Lint, type-check, tests and build all passed when I ran them.
10. **Observability basics.** Structured Serilog logging with an optional MongoDB sink, correlation-ID middleware, and `/health` and `/health/ready`, which check both databases and role-key migration state.
11. **Compose takes secrets from the environment.** `docker-compose.yml` requires them with `${VAR:?…}` rather than hard-coding them, and `.env` is git-ignored.
12. **Unusually thorough documentation.** Architecture, business perspective (personas, scope), an end-user guide, a frontend guide, and contribution and branching rules.

---

## Cons

Severity: **Critical** = exploitable or data-destroying, fix before showing anyone. **High** = a reviewer will notice and it undermines the project's credibility. **Medium** = should fix. **Low** = polish.
Effort: **S** ≤ half a day, **M** 1–3 days, **L** > 3 days.

### C1 — Critical — Being logged in is enough to change most of the system; there is no role check on half the API
- **Evidence.** These controllers have no role, capability or ownership check. They rely only on the global "authenticated user" filter, and their services do no checks either:
  - `OrganizationsController` (create, update, delete, logo upload)
  - `DepartmentsController`, `LocationsController`, `SitesController` (8 actions), `JobPositionsController`
  - `JobPositionRolesController` (see C2)
  - `DocumentsController` (see C3)
  - `AttendanceController` (12 actions; check-in, check-out and manual entry for any `employeeId` in the body, e.g. `CheckInRequestModel.EmployeeId`)
  - `MenusController` (create, update, delete under a bare `[Authorize]`)
  - `LeaveTypesController`, `LeavePolicyRulesController`, `LeaveOperationsController` (accrual and year reset), `LeaveImportsController`

  The only gate is the menu shown in the UI, which is hidden but not enforced. Meanwhile `Pukar.Usermanagement.API/Controllers/AuthController.cs:23-24` exposes `POST /api/auth/register` as `[AllowAnonymous]`, and `AuthService.RegisterAsync` creates an active user and returns tokens straight away.
- **Impact.**
  - Anyone who can reach the API can register and then change or delete organisational data, run leave accrual, and record attendance for other people.
  - For an HR system this is the first thing a technical interviewer will test.
- **Recommended fix.**
  - Add named authorisation policies (for example `OrgAdmin`, `LeaveAdmin`, `AttendanceAdmin`) backed by the existing role-key capability model, and apply them to every mutating endpoint and every endpoint that reads other people's data.
  - Make "self" endpoints (attendance check-in) work out the employee from the token, not from the request body.
  - Turn off or invite-gate public registration, since employees already get invitations.
  - Add one test per controller asserting that a user with no role gets 403.
- **Effort.** M.

### C2 — Critical — Any logged-in user can give the ADMIN role to any job position (privilege escalation)
- **Evidence.**
  - `EMS.API/Controllers/JobPositionRolesController.cs` (`PUT api/JobPositions/{id}/roles`) has no access check.
  - `PositionRoleService.SetPositionRolesAsync` (`EMS.Application/Services/JobPositions/PositionRoleService.cs:80-141`) saves the role keys and then calls `_employeeRoleSyncService.SyncEmployeesForPositionAsync`. That pushes the roles to every employee in the position through User Management.
  - Compare `RoleKeyPermissionsController`, which correctly uses `[Authorize(Policy = "AdminAccess")]`.
- **Impact.** An employee can make their own job position (and so themselves, at next login or refresh) ADMIN. That is full takeover of the system.
- **Recommended fix.** Require `AdminAccess` (or a dedicated `roles.manage` capability) on both actions. Add a unit or integration test that a non-admin gets 403.
- **Effort.** S.

### C3 — Critical — Employee documents and leave attachments can be downloaded without logging in
- **Evidence.**
  - `LocalDocumentFileStorage.SaveAsync` writes to `wwwroot/attachments/Employee/{id}/Documents/`, and `LocalLeaveAttachmentStorage` writes to `wwwroot/attachments/Leave/{id}/`.
  - `Program.cs:268` calls `app.UseStaticFiles()` **before** `UseAuthentication()` (line 272), so anything under `wwwroot` is served anonymously.
  - The storage path is returned to clients (`DocumentResponseModel.StoredRelativePath`, `LeaveRequestAttachmentResponseModel.StoragePath`).
  - `GET /api/Documents` with no `employeeId` lists every document, with no access check (see C1).
- **Impact.** Identity documents, visas, contracts and medical certificates are personal information. Anyone with a link, or any registered user who lists documents, can fetch them. This also breaks the project's own rule to "keep database records small… return path" without thinking about privacy.
- **Recommended fix.**
  - Store private files **outside** `wwwroot` (for example a configured `Storage:PrivateRoot`). Keep only the organisation logo public.
  - Serve files only through the authorised `GET …/{id}/file` endpoints, after an access check.
  - Remove storage paths from response DTOs and return a download URL instead.
  - Update `shared-attachment-storage-convention.mdc` to separate public and private attachments.
- **Effort.** M.

### C4 — Critical — Leave accrual can be run repeatedly by anyone, and each run adds leave again
- **Evidence.**
  - `LeaveOperationsController` (`POST api/LeaveOperations/accrual/run` and `year-reset/run`) has only `[Authorize]`.
  - `LeaveAccrualService.RunAccrualAsync` (`EMS.Application/Services/Leave/LeaveAccrualService.cs:25-102`) does `balance.AccruedAmount += accrual` on every call, with no record of which period has already accrued.
  - `LeaveBalance` has no concurrency token, unlike `LeaveRequest`.
- **Impact.**
  - Leave balances can be inflated freely, by accident (a double-click or retry) or on purpose.
  - Concurrent approvals and accruals can lose updates.
  - Leave balances are a high-risk area under `DEV-ORG.md`.
- **Recommended fix.**
  - Restrict both endpoints to a leave-admin policy.
  - Add an accrual-run ledger (organisation, period, leave type), unique per period, so a repeated run does nothing; or store per-period accrual transactions.
  - Add `RowVersion` to `LeaveBalance`.
  - Load balances in one query rather than per employee per policy (also fixes the N+1 at lines 47-55).
- **Effort.** M.

### C5 — High — The self-service "leave" permission also lets staff manage everyone else's leave (verified)
- **Evidence.**
  - In `EMS.Application/Services/Leave/LeaveEmployeeAccessService.cs:42-62`, `CanManageOtherEmployeesLeaveAsync` returns true if the caller's roles may see the menu with key `"leave"`.
  - Line 12 calls that key "the self-service Leave page", and the seed confirms it is the top-level Leave menu (`EmsRbacSeedHostedService.cs:39`); `leave.admin` is a separate child menu.
  - `CanAccessEmployeeInternalAsync` lets the caller in when the target is themselves **or** when they pass that check.
  - It guards `LeaveRequestService` (get, create, cancel), `LeaveBalanceService` (view balances) and `LeaveAttachmentService` (upload, download).
- **Impact.** Any role given the normal Leave page (which every employee needs) can view balances, view or create or cancel leave requests, and download medical certificates for any employee.
- **Recommended fix.**
  - Check `leave.admin` (or a dedicated capability such as `leave.manage-others`) instead of `leave`.
  - For managers, reuse the direct-report scoping already in `LeaveApprovalAccessService`.
  - Add tests: an employee with only `leave` is denied for another employee; `leave.admin` is allowed.
- **Effort.** S.

### C6 — High — File uploads accept dangerous file types that the server then serves from its own address
- **Evidence.**
  - `LocalDocumentFileStorage.TryResolveFileKind` accepts a file if the **content type or** the extension looks right (for example `ct.Contains("pdf") || ext == ".pdf"`), and then **keeps the uploaded extension**. A file `x.html` sent as `application/pdf` is stored as `.html`.
  - `LocalLeaveAttachmentStorage.ValidateContentOrThrow` checks only the size, not the type.
  - `LocalOrganizationLogoStorage` allows `.svg` (line 11).
  - All of these are served by static files from the API's address (C3).
  - There is no magic-byte check, no `X-Content-Type-Options: nosniff`, and no `Content-Disposition: attachment` on the static paths.
- **Impact.** Stored cross-site scripting (XSS) and malware hosting on the API's address.
- **Recommended fix.**
  - Use an allow-list of extension **and** content type **and** magic bytes, and save files with an extension the server chooses.
  - Drop SVG, or sanitise it.
  - Serve files through controllers with `Content-Disposition: attachment` and `nosniff`.
  - Move the validation into one shared Infrastructure file-storage service.
- **Effort.** S–M.

### C7 — High — No protection against password guessing
- **Evidence.**
  - There is no account lockout or failed-attempt counter in User Management (searched for `lockout`, `AccessFailed`, `FailedLogin`: none found).
  - There is no `AddRateLimiter` or `UseRateLimiter` in `Program.cs`.
  - The only throttling is a static per-client dictionary for service tokens (`ServiceAuthService.cs:12`).
  - Login, register and forgot-password are anonymous.
- **Impact.** Unlimited credential stuffing and password-reset email flooding. A reviewer will expect this in an auth module.
- **Recommended fix.**
  - Use ASP.NET Core rate limiting (fixed window per IP and per email) on `/api/auth/*`.
  - Add progressive lockout after N failures, with tests.
- **Effort.** S–M.

### C8 — High — The test suite is thin, partly fake, not run automatically, and would currently fail
- **Evidence.**
  - Unit tests (`EMS.Application.UnitTests`, 22 files) cover Employees, Leave request, type, policy and approval, Manager, Notifications and Onboarding only.
  - There are **no** tests for Attendance, Documents, Organisations, Departments, Sites, Locations, Job positions and position roles, Tasks, Shifts, Leave accrual and balances, `LeaveEmployeeAccessService`, the audit interceptor or the outbox.
  - `EMS.API.IntegrationTests/EmployeeAccessIntegrationTests.cs:11` is `Assert.That(true, Is.True, …)`. `UnitTest1.cs` is a template stub.
  - The architecture test `EmsControllers_DoNotDependOn_DbContextDirectly` checks for any dependency on `Microsoft.EntityFrameworkCore`, but `DepartmentsController.cs:6,66` imports it and catches `DbUpdateException`. So the test should fail, which suggests the tests are not run regularly.
  - There is no `.github/workflows`; `CONTRIBUTING.md` lists CI as optional.
  - `README.md:203` says "Automated tests are not included yet".
  - The latest feature (manager leave approval, HEAD) was delivered without compiling or running any .NET code (decision log of run `20261115-2052`).
- **Impact.** There is no proof that the security rules or business rules hold. A hiring manager sees placeholder tests.
- **Recommended fix.**
  - Add GitHub Actions running `dotnet build`, `dotnet test` and `npm ci && npm run lint && npm test && npm run build` on every pull request.
  - Replace the placeholder with real `WebApplicationFactory` tests (Testcontainers SQL, or SQLite for the auth matrix) that cover C1–C5.
  - Fix the `DepartmentsController` violation by mapping the conflict in the service.
  - Add tests for accrual and attendance.
  - Publish a coverage badge.
- **Effort.** L (CI alone is S).

### C9 — High — The Docker "one command" setup is broken and starts with a publicly known admin password
- **Evidence.**
  - `EMS.API/Dockerfile.compose` copies only API, Application, Domain, Infrastructure, `Pukar.Shared` and `Pukar.Usermanagement.Contracts`.
  - `EMS.API.csproj` also references `Pukar.Notifications.Application`, `Pukar.Usermanagement.Application`, `.Infrastructure`, `.Domain` and `.API`, so `dotnet restore` or `publish` in the image will fail.
  - `docker-compose.yml:85` runs the API with `ASPNETCORE_ENVIRONMENT: Development`. That loads `appsettings.Development.json`, which turns on `SeedAdmin` with `admin@localhost` / `Admin123!@#` (lines 17-21), and also auto-migrates.
  - `depends_on` does not wait for SQL to be healthy.
- **Impact.** "Clone, then `docker compose up`" (the first thing a reviewer tries) fails. If it is fixed naively, it ships a known admin login.
- **Recommended fix.**
  - Copy all referenced projects (or the whole solution, using `.dockerignore`).
  - Add `condition: service_healthy`.
  - Use a separate `Docker` environment whose seed admin password comes from `.env`.
  - Add a smoke step in CI that builds the images.
- **Effort.** S.

### C10 — High — Development secrets and a default admin password are committed to the repository
- **Evidence.** `EMS.API/appsettings.Development.json`:
  - lines 3-4: SA password `Dev123!@#Strong`
  - line 12: JWT signing key
  - line 21: seed admin password `Admin123!@#`

  The README itself recommends User Secrets.
- **Impact.**
  - Reviewers flag committed secrets automatically (GitHub secret scanning, Gitleaks).
  - The same values go into Docker (C9).
- **Recommended fix.**
  - Move them to User Secrets or `.env`, keep a `appsettings.Development.example.json`, and rotate the values.
  - Make the app refuse to start if the signing key matches the sample.
  - Add a Gitleaks step to CI.
- **Effort.** S.

### C11 — Medium — Documentation and coding rules contradict the current code
- **Evidence.**
  - `.cursor/rules/ems-architecture-layers.mdc:16,25` says EMS must reference only UM Contracts and `Pukar.Shared`, and that identity goes "through `IEmployeeUserManagementGateway` (HTTP)". In fact Infrastructure and API reference UM Application, Domain, Infrastructure and API, and the gateway is `InProcessEmployeeUserManagementGateway`. The architecture tests encode the new, in-process rule.
  - README:
    - says documents live in `wwwroot/uploads/documents/` (line 180); the code uses `attachments/Employee`
    - says `dev:all` starts the "User Management Host" (line 151)
    - says there are no tests (line 203)
    - its endpoint table omits Leave, Attendance, Tasks, Shifts and Onboarding
  - `DEV-ORG.md:3` still has the placeholder `.NET <version — check your .csproj>` (it is net9.0).
  - Redis is in the stack, config and compose, but **no code uses it** (no `IDistributedCache` or StackExchange.Redis).
  - `docs/ems-um-http-integration.md` describes the retired HTTP design.
- **Impact.** Reviewers and AI agents follow the docs and get things wrong. Claiming Redis without using it looks like padding the CV.
- **Recommended fix.**
  - Make one pass to align the rules, README and DEV-ORG with the single-host design.
  - Either implement one real Redis use (for example caching permission and menu lookups) or remove Redis from compose and from the stack claims.
  - Mark retired docs as historical.
- **Effort.** S–M.

### C12 — Medium — Errors are handled by matching the text of exception messages
- **Evidence.**
  - 99 `catch (BusinessRuleException)` blocks across controllers.
  - `EmployeeControllerHelpers.HandleBusinessRule` decides on 403, 404 or 409 by comparing `ex.Message` with string constants, or by `Contains("not found")` or `Contains("already exists")`. `LeaveBalancesController:59` does the same, in 19 places in total.
  - Status codes are inconsistent: Attendance returns 409 for validation errors, Departments returns plain-string `Conflict(ex.Message)`, and others return `{ message }`.
  - There is no global exception handler or ProblemDetails.
- **Impact.**
  - Controllers are not thin: 540 lines in `EmployeesController`.
  - Changing a message text silently changes HTTP behaviour.
  - Clients get inconsistent error shapes.
- **Recommended fix.**
  - Use typed exceptions (`AccessDeniedException`, `NotFoundException`, `ConflictException`) or a `Result<T>`.
  - Map them in one `IExceptionHandler` to RFC 7807 ProblemDetails.
  - Delete the per-action try/catch blocks.
- **Effort.** M.

### C13 — Medium — The Application layer depends directly on EF Core and the DbContext
- **Evidence.**
  - 43 Application files import `Microsoft.EntityFrameworkCore` and call `ToListAsync` or `AnyAsync` on `IBaseRepository<T>.GetQueryable()`.
  - `LegacyRoleKeyGuard` (`EMS.Application/Services/Authorization/LegacyRoleKeyGuard.cs:24-37`) injects `AppDbContext` directly.
  - `AppDbContext`, migrations and the EF SqlServer provider live in **Domain**.
  - The file-storage services live in API rather than Infrastructure.
- **Impact.** This is not true onion architecture, and an interviewer will ask about it. The persistence technology leaks into business logic. The existing architecture tests do not catch it.
- **Recommended fix.** This does not need a rewrite. Either:
  - (a) document it as a deliberate, pragmatic choice ("IQueryable repository, DbContext in Domain for migrations") in `ARCHITECTURE_AND_PATTERNS.md`, or
  - (b) gradually move `AppDbContext` and migrations to Infrastructure, add specific repository methods for hot paths, and add an architecture test forbidding `AppDbContext` in Application.

  At minimum, remove the direct `AppDbContext` use in `LegacyRoleKeyGuard`.
- **Effort.** S for (a), L for (b).

### C14 — Medium — Login tokens are kept in browser storage that any injected script can read
- **Evidence.** `ems-web/src/shared/auth/auth-storage.ts:21-60` keeps the access token, the **7-day refresh token** and the user in `localStorage`.
- **Impact.** Any XSS (see C6) can steal a long-lived session. This is a common interview question.
- **Recommended fix.**
  - Move the refresh token to an `HttpOnly; Secure; SameSite=Strict` cookie set by `/api/auth/refresh` and keep the access token in memory; or
  - at least document the trade-off and add a strict Content-Security-Policy in `next.config.ts`.
- **Effort.** M.

### C15 — Medium — Known-vulnerable frontend packages
- **Evidence.**
  - `npm audit --omit=dev` (run 15 November 2026) found 33 vulnerabilities: 2 critical, 17 high, 10 moderate, 4 low.
  - They include **direct** `next@16.2.2` (critical), `axios` (high) and `zod` (high).
  - The `shadcn` CLI sits in `dependencies` (not `devDependencies`) and pulls in `@modelcontextprotocol/sdk`, `hono`, `body-parser` and similar.
- **Impact.** Badges and Dependabot will show red, and it makes the production image bigger.
- **Recommended fix.**
  - Upgrade `next`, `axios` and `zod`.
  - Move `shadcn` and `@svgr/webpack` to devDependencies.
  - Enable Dependabot.
  - Re-run the audit.
- **Effort.** S.

### C16 — Medium — No caching, some unpaged lists and N+1 loops
- **Evidence.**
  - There is no caching at all (C11), even though permission and menu checks run on every request (`PermissionEvaluator`, menu ID lookups such as `LeaveEmployeeAccessService.ResolveLeaveMenuIdAsync`).
  - `GET /api/Documents`, `GET /api/Employees` (obsolete), and the attendance employee and report lists return everything without paging (only the employee directory is paged).
  - The accrual has an N+1 query (C4). `BaseRepository.GetAllAsync` loads whole tables.
- **Impact.** Fine at demo size, but a reviewer will spot it.
- **Recommended fix.**
  - Add a short-lived cache (memory or Redis) for role-to-menu and capability maps, cleared on change.
  - Add paging to the documents and attendance lists.
  - Use set-based queries in batch jobs.
- **Effort.** M.

### C17 — Medium — The last feature skipped the project's own high-risk checks
- **Evidence.**
  - The `20261115-2052` decision log shows manager leave approval touched permissions, leave balances and an EF migration (all listed as high-risk in `DEV-ORG.md`).
  - It was overridden to Low risk, so every gate was automatic.
  - The migration `20261115100000_AddLeaveRequestDecisionFields.cs` and the snapshot edit were written by hand and never compiled.
- **Impact.** The repository HEAD may not build. The hand-edited model snapshot can drift and break the next `migrations add`.
- **Recommended fix.**
  - Run `dotnet build`, `dotnet test` and `dotnet ef migrations has-pending-model-changes` locally before merging `dev-org/manager-leave-approval`.
  - Let CI (C8) enforce this from now on.
  - This is the CEO's call; it is flagged, not judged.
- **Effort.** S.

### C18 — Medium — Leftover scaffolding from the migration period makes the codebase harder to read
- **Evidence.**
  - `AuthorizationCutoverController`, `AuthorizationTelemetryController`, `RoleKeyBackfillHostedService`, `RoleKeyMigrationHealthCheck`, `Authorization:EnableLegacyRoleIdFallback`
  - the service-token JWT scheme kept "temporarily" (`UserManagementJwtExtensions.cs:52-66`)
  - the retired `Pukar.Usermanagement.Host` project, `tools/UmDbSplitMigrator`, and `UserManagementIdempotencyCleanupHostedService`, all still in the solution (22 projects)
- **Impact.** A reviewer has to work out what is live and what is historical, and the live attack surface is larger than it needs to be.
- **Recommended fix.**
  - Once the cutover is confirmed, delete or archive the cutover and telemetry endpoints, the service-token scheme and the Host project.
  - Move one-off tools into a `tools/` solution folder or out of the main solution.
  - Keep the runbooks under `docs/history/`.
- **Effort.** M.

### C19 — Low — Two screens show raw database IDs to users, against the project's rule
- **Evidence.**
  - `ems-web/src/features/employees/components/PositionTransferForm.tsx:58`: "Current position ID: {employee.jobPositionId}"
  - `DepartmentTransferForm.tsx:58`: "Current department ID: …"
  - Both break `ems-ui-no-id-display.mdc`.
- **Impact.** Minor polish, but it breaks a documented rule.
- **Recommended fix.** Show the current position or department name (it is already in the lookup lists), or "Not assigned".
- **Effort.** S.

### C20 — Low — Template leftovers and stray files
- **Evidence.**
  - `EMS.API/WeatherForecast.cs` and `Controllers/WeatherForecastController.cs`
  - `EMS.Domain/Class1.cs`, `EMS.Infrastructure/Class1.cs`
  - `EMS.Application.UnitTests/UnitTest1.cs`
  - `EMS.API/EMS.API.csproj.user`
  - `CreateDocumentForm` declared inside `DocumentsController.cs` (breaks the one-type-per-file rule)
  - The Claude Project snapshot also contains build output (`artifacts/tmp-api-build/*.dll`, `EMS.API/bin/TempBuild`, `EMS.API/obj/VerifyBuild`). `artifacts/` is not in `.gitignore`.
- **Impact.** Looks unfinished. The binaries waste context for the AI team.
- **Recommended fix.** Delete the leftovers, add `artifacts/` to `.gitignore`, and refresh the Project files without bin, obj or artifacts.
- **Effort.** S.

### C21 — Low — Local Docker hardening
- **Evidence.**
  - `mongo-express` is published on port 8081 with `ME_CONFIG_BASICAUTH: "false"`.
  - SQL (1433), Mongo (27017) and Redis (6379) are all published to the host.
  - The API image runs as root and has no `HEALTHCHECK` (the web image correctly uses `USER nextjs`).
- **Impact.** Fine on a laptop, but worth tidying for a "production-ready" impression.
- **Recommended fix.**
  - Put mongo-express behind a `tools` profile with basic auth.
  - Bind database ports to `127.0.0.1`.
  - Add `USER app` and a `HEALTHCHECK` hitting `/health`.
- **Effort.** S.

### C22 — Low — Frontend robustness and size
- **Evidence.**
  - There are no route-level `error.tsx`, `global-error.tsx` or `not-found.tsx` files under `src/app`.
  - Some very large components: `EmployeeProfilePage.tsx` (859 lines), `CreateEmployeeWizard.tsx` (488), `UsersSection.tsx` (451).
  - Frontend tests (35) cover utilities only, with no component or flow tests.
  - Accessibility relies mostly on shadcn/base-ui primitives (31 of 150 `.tsx` files use `aria-*`). I did not run an a11y audit or a responsive check.
- **Impact.** An unhandled render error blanks the page, and large files are hard to review.
- **Recommended fix.**
  - Add error boundaries.
  - Split the profile page into tab components.
  - Add a few React Testing Library tests for login, leave request and approval.
  - Run `@axe-core` or Lighthouse once and fix what it finds.
- **Effort.** M.

### C23 — Low — Portfolio presentation gaps
- **Evidence.** The README opens with "Backend API for employee and organization directory data", which undersells it. There are no screenshots, no demo GIF, no "what this demonstrates" section, no live demo or seeded demo data, and no CI or coverage badges.
- **Impact.** A hiring manager spends about 60 seconds on a README and won't see how much the project covers.
- **Recommended fix.**
  - Rewrite the top of the README: one paragraph summary, feature list, architecture diagram (already exists), 3–4 screenshots, "Run in 2 minutes" (after C9), and a "Design decisions" section (outbox, audit interceptor, gated migrations, honest trade-offs from C13).
  - State clearly that it is a personal portfolio project.
- **Effort.** S–M.

---

## Quick wins (each ≤ half a day, highest value first)

1. **C2:** add `[Authorize(Policy = "AdminAccess")]` to `JobPositionRolesController`. This closes the privilege escalation.
2. **C5:** change `LeaveMenuKey` in `LeaveEmployeeAccessService` from `"leave"` to `"leave.admin"` (or a capability), and add two tests.
3. **C1/C4 stopgap:** add `[Authorize(Policy = "AdminAccess")]` to `LeaveOperations`, `LeaveImports`, `LeaveTypes`/`LeavePolicyRules` writes, `Menus` writes, `Organizations`, `Departments`, `Sites`, `Locations` and `JobPositions` writes, and `Documents`. Turn off anonymous `/api/auth/register`.
4. **C3 stopgap:** add a static-file guard so `/attachments/Employee` and `/attachments/Leave` are not served by `UseStaticFiles` (or move `UseStaticFiles` after auth with a policy). Remove `StoredRelativePath` and `StoragePath` from the DTOs.
5. **C10:** move dev secrets to User Secrets or `.env` and rotate them.
6. **C9:** fix `Dockerfile.compose` to copy all referenced projects, and remove the known seed password from the Docker path.
7. **C8 (part):** add a GitHub Actions workflow (dotnet build/test plus npm lint/test/build), delete the placeholder tests, and fix the `DepartmentsController` EF import so the architecture tests pass.
8. **C7 (part):** add `AddRateLimiter` with a fixed-window policy on `/api/auth/*`.
9. **C15:** upgrade `next`, `axios` and `zod`; move `shadcn` to devDependencies.
10. **C11/C19/C20:** fix the README, DEV-ORG and rules drift; remove WeatherForecast, Class1 and UnitTest1; fix the two raw-ID labels; add `artifacts/` to `.gitignore`.
