# 02 — EMS System Capability Inventory (System Analyst)

Run: 20261115-2230-EMS-strategy · Date: 15 November 2026 · Source: read-only working copy `/home/claude/work/ems` (HEAD `e053772 impl`, which carries the manager leave approval change; the local branch is named `master`).
Scope: what EMS does today, taken from the code. EMS is a personal portfolio project. All paths below are relative to the repo root.

Maturity key: **Complete**: the end-to-end flow works through the UI with server-side rules. **Partial**: works, but there are clear gaps such as API-only pieces, missing authorisation or missing UI. **Stub**: placeholder or scaffold only.

---

## 0. Platform at a glance

| Item | Evidence |
|---|---|
| Backend: ASP.NET Core **net9.0**, a single host `EMS.API` with User Management (UM) running in-process | `EMS.API/EMS.API.csproj`, `README.md` "Single-host architecture" |
| Two SQL Server databases: `EMSDevDB` (`AppDbContext`) and `UserManagementDb` | `README.md`, `docker-compose.yml` |
| Onion layers: Domain / Application / Infrastructure / API, enforced by architecture tests | `EMS.ArchitectureTests/UserManagementBoundaryTests.cs` |
| 33 EMS migrations (Mar–Nov 2026). The latest is `20261115100000_AddLeaveRequestDecisionFields`, which was hand-written and has no `.Designer.cs`, although its `[Migration]` attribute is present | `EMS.Domain/Database/Migrations/` |
| 34 EMS controllers, about 150 EMS endpoints, plus about 35 UM endpoints | `EMS.API/Controllers/`, `Pukar.Usermanagement/Pukar.Usermanagement.API/Controllers/` |
| Frontend: Next.js 16.2 / React 19, React Query, Zustand, react-hook-form + zod, shadcn, react-big-calendar | `ems-web/package.json` |
| 33 App Router pages and 20 feature folders | `ems-web/src/app/**/page.tsx`, `ems-web/src/features/*` |
| Global auth: every controller requires an authenticated user through a global `AuthorizeFilter`. There is also an `AdminAccess` policy | `EMS.API/Program.cs:108`, `EMS.API/Auth/UserManagementJwtExtensions.cs:73` |

---

## 1. Module inventory

### 1.1 Employees (master data and lifecycle)
**Exists**
- CRUD, plus a paged **directory** with filters, a **CSV export** and **possible-duplicate detection**: `EMS.API/Controllers/EmployeesController.cs` (GET `/directory`, `/export`, `/possible-duplicates`). Services: `EmployeeDirectoryService.cs` (CSV built by hand) and `EmployeeService.cs`.
- Employee number auto-allocated from a per-organisation sequence (`EMP###`): `SqlEmployeeNumberAllocator.cs`, migration `AddOrganizationEmployeeNumberSequences`.
- Relationship validation (department, position, manager and location must belong to the same organisation): `EmployeeRelationshipValidator.cs`.
- Lifecycle actions: terminate, archive, restore and employment-status change. Delete archives the record (soft delete) and sets `RetentionUntilUtc` from `EmployeeRetentionPolicy`. See `EmployeeLifecycleService.cs:235` and `EmployeeService.cs:646`.
- Transfers between departments, positions and managers, with effective-dated history tables: `EmployeeTransferService.cs`, DbModels `Employee{Department,Position,Manager,EmploymentStatus}History`, `EmployeeHistoryController.cs`.
- **Scheduled (future-dated) changes** of 7 types (Terminate, Archive, RestoreRecord, ChangeEmploymentStatus, Transfer×3). They are applied by `EmployeeScheduledChangeWorker`, and `ScheduledChangeReminderWorker` sends reminders. The business time zone defaults to `Australia/Sydney` (`EMS.Application/Options/EmployeeSchedulingOptions.cs`).
- Identity linking: every new employee gets an inactive UM account plus an invitation through the outbox (`EmployeeIdentityProvisioningService.cs`, `IntegrationOutboxDispatcher.cs`). Invitations can be listed, resent or revoked. Login can be reactivated, a user linked, and linked-user roles set.
- UI: `/employees` (table, filters, **CreateEmployeeWizard**), `/employees/[id]` (profile tabs, history modal, invitation panel, lifecycle dialogs, upcoming changes, onboarding progress, documents), `/employee-transfers`.

**API only / UI only**
- `GET /api/Employees/{id}/sites` has no UI caller.
- `/employee-transfers` is in the static fallback nav (`ems-web/src/shared/components/layout/ems-nav-items.ts`) but **not in the RBAC menu seed**, so the API-driven sidebar never shows it. It is reachable only through a link in `EmployeeForm.tsx:320`.

**Gaps**
- The employee record holds only identity and organisation fields. `EMS.Domain/DbModels/Employee.cs` has **no** address, emergency contact, employment type (full-time, part-time or casual), standard hours, award/classification, pay rate, TFN, super or bank fields.
- There is no bulk employee import.

**Maturity:** Complete for core HR records. Partial against a full HRIS.
**Tests:** strongest coverage in the repo. `EMS.Application.UnitTests/Employees/*` has 10 files: service, directory, lifecycle, transfer, scheduling, relationship validator, number allocator, access, identity provisioning.

### 1.2 Organisation structure (organisation, departments, locations, job positions, sites)
**Exists**
- **Organisation**: CRUD plus logo upload (`OrganizationsController.cs`, `LocalOrganizationLogoStorage.cs`). There are setup flows at `/setup` and `/organization/setup`, and the `OrganizationGate` provider blocks the app until an organisation exists.
- **Departments**: CRUD with a parent/child hierarchy (`Department.ParentDepartmentId`). UI at `/departments`.
- **Job positions**: per-organisation CRUD (`JobPositionsController.cs`), plus **position→role-key mapping** (`JobPositionRolesController.cs`, `PositionRoleService.cs`), which drives employee role assignments with `EmployeeRoleSource`. UI at `/positions`.
- **Sites**: CRUD and employee↔site assignment (`SitesController.cs` `/{siteId}/employees`). UI at `/sites`.
- **Locations**: CRUD API (`LocationsController.cs`).

**API only**
- Locations has **no management page**. The UI only reads the list for employee form dropdowns (`ems-web/src/features/locations/services/locationService.ts` is GET only).
- Site↔employee assignment endpoints have no UI caller (`siteService.ts` has CRUD only).
- Organisation DELETE has no UI.

**Gaps**
- `Site` has **no `OrganizationId`** (`EMS.Domain/DbModels/Site.cs`), so it is a global table.
- None of these controllers or services check anything beyond authentication. For example, `DepartmentService.cs`, `SiteService.cs`, `OrganizationService.cs` and `JobPositionService.cs` contain no access checks, so any signed-in user can change the org structure.

**Maturity:** Partial.
**Tests:** none for these services.

### 1.3 Onboarding
**Exists**
- Organisation-level checklist templates with items and categories (`OnboardingChecklistTemplatesController.cs`, Admin role for writes).
- Checklists and tasks are generated when an employee is created in **Preboarding** status (`OnboardingChecklistService.cs:28-35`). Tasks link back through `TaskItem.OnboardingTemplateItemId`.
- `GET /api/employees/{id}/onboarding` shows progress.
- UI: `/organization/onboarding` and the progress widget on the employee profile.

**Gaps**
- No offboarding checklist.
- No recruiting, requisition or offer stages (stated out of scope in `docs/business-perspective.md` §4).
- Items are completed only through Tasks.

**Maturity:** Complete for the defined scope.
**Tests:** `Onboarding/OnboardingChecklistServiceTests.cs`; frontend `features/employees/utils/onboardingProgress.test.ts`.

### 1.4 Documents
**Exists**
- Document types, plus employee document upload, edit, delete and download (PDF, Word, images; 16 MB limit). Files are stored under `wwwroot/attachments/Employee/{id}/Documents/{guid}`, linked through the `EmployeeDocument` table.
- Issue and expiry dates, with `DocumentExpiryNotificationWorker` sending in-app reminders at set thresholds.
- UI: the documents section on the employee profile (`features/documents/components/*`).

**Gaps / risks**
- `DocumentService.cs` has **no access checks**, so any authenticated user can list or download any employee's documents by ID.
- Files sit under `wwwroot` with `app.UseStaticFiles()` placed **before** `UseAuthentication` (`Program.cs:268-273`). Employee documents and leave attachments can therefore be fetched **anonymously** by anyone who has the URL. GUID file names are the only protection.
- `DocumentType` is global (no organisation) and there is no CRUD for types.
- There is no document management page outside the employee profile.

**Maturity:** Partial.
**Tests:** frontend `features/documents/services/documentService.test.ts` only.

### 1.5 Attendance
**Exists**
- Check-in and check-out with optional GPS (browser geolocation, `AttendanceSection.tsx:18`), breaks, and manual entry with a reason (`AttendanceController.cs`, `AttendanceService.cs`, 591 lines).
- Summaries, daily and period (week/month) reports, punctuality (late/early) and absenteeism analytics. Export to **CSV/XLSX/PDF** uses ClosedXML and QuestPDF (Community licence, `AttendanceService.cs:34`).
- `AttendancePolicy` (standard daily and break minutes, overtime flag) feeds the calculations.
- UI: `/attendance`, `/attendance/reports`, `/attendance/analytics`.

**Gaps / risks**
- **No authorisation at all.** `AttendanceController` is authenticate-only and `CheckInRequestModel` takes any `EmployeeId`, so a user can clock in, clock out or enter manual time for anyone and read anyone's reports.
- The UI is admin-style: it uses an employee picker (`AttendanceSection.tsx:35-40`) rather than "me".
- Attendance does not link to Shifts.
- No timesheet approval and no overtime or penalty calculation.
- No API or UI to maintain `AttendancePolicy`.
- `AttendanceStatus.AutoApproved` exists but there is no approval flow.

**Maturity:** Partial (rich reporting, weak controls).
**Tests:** none.

### 1.6 Shifts (rostering)
**Exists**
- Shift CRUD (Admin role only, `ShiftsController.cs:11`) with overlap prevention and an organisation-match rule (`ShiftService.cs:35,194`).
- Status: Scheduled → Started → Completed or Cancelled.
- The employee can start a shift from the portal (`EmployeePortalController` `shifts/{id}/start`).
- UI: `/shifts` table and `/shifts/calendar` (react-big-calendar).

**Gaps**
- No recurring shifts, templates or publish step. No availability or swap requests.
- The portal has no "complete shift" action.
- `/shifts/calendar` is not in the menu seed.

**Maturity:** Partial (basic rostering).
**Tests:** frontend `features/shifts/services/shiftService.test.ts` and `types/shift-form.schema.test.ts`. No backend tests.

### 1.7 Leave
**Exists**
- **Leave types** per organisation (days or hours, requires-attachment flag). CRUD API and UI at `/leave/admin/settings` (`LeaveSettingsSection.tsx`).
- **Policy rules**: accrual rate per period, frequency (Monthly/PayCycle/Yearly), maximum carry-forward, proration, effective dates (`LeavePolicyRule.cs`).
- **Balances** with opening, accrued, used, adjusted and carry-forward components.
- **Requests**: create, update (`ModifiedPending`) and cancel, with balance-sufficiency and overlap validation. Attachments go under `wwwroot/attachments/Leave/`.
- **Manager approval workflow (new)**: `POST /api/LeaveRequests/{id}/approve|reject` and `GET /api/Manager/leave-approvals`, handled by `LeaveApprovalService.cs` and `LeaveApprovalAccessService.cs`.
  - The approver must be the direct manager (`Employee.ManagerId`) or a leave admin (`leave.admin` menu or the ADMIN role).
  - Self-approval is blocked and the reviewer must have a linked employee record.
  - A rejection reason is required.
  - A `RowVersion` concurrency token returns 409 on conflict.
  - Approval moves the amount into `UsedAmount`. Cancelling approved leave refunds it (`LeaveRequestService.cs:179-189`).
  - Approved and rejected notifications are sent.
  - UI: `ManagerLeaveApprovalsPanel.tsx` on `/manager/team`.
- Accrual run and leave-year reset (`LeaveOperationsController.cs`, `LeaveAccrualService.cs`).
- Bulk import of **leave requests** from CSV lines (`LeaveImportService.cs`, `/leave/admin`).
- Admin summary counts.
- Self-service at `/employee-portal/leave`.

**API only**
- Policy-rule create, update and delete. The UI calls only the GET, and its result is never rendered (`useLeavePolicyRules` has no component consumer).
- Accrual run and year-reset have **no UI and no scheduler**.
- `GET LeaveBalances/employee/{id}/type/{typeId}`.
- Admins can approve through the API, but there is **no admin approval queue** in the UI. The only queue is the manager's direct-reports list.

**Defects / risks found**
- **Year-reset double-counts carry-forward.** `LeaveAccrualService.cs:137-140` sets both `OpeningBalance = carryForward` and `CarryForwardAmount = carryForward`. The available-balance formula adds both (`LeaveApprovalService.cs:226-231`), so after a reset the available balance is 2 × the carried amount.
- **Accrual is not idempotent.** Running it twice for the same period adds the accrual twice (`LeaveAccrualService.cs:84`), because no last-accrued-period is stored.
- Proration always uses calendar days in the month, whatever the frequency is.
- `RequestedAmount` is supplied by the client. Nothing calculates working days or public holidays, and there is no part-day logic.
- There is no balance adjustment or opening-balance endpoint, so balances can come only from accrual runs.
- Leave types, policy rules, accrual, year-reset and bulk import have **no role checks**. `LeaveTypeService`, `LeavePolicyRuleService`, `LeaveAccrualService` and `LeaveImportService` have none, so any authenticated user can run accrual or edit policies.
- Docs are out of date. `docs/leave_non_workflow_contract.md` and `docs/leave_release_checklist.md` still say "approval workflow deferred", and the `/leave/admin` subtitle reads "Policy and import operations (workflow deferred)." (`LeaveAdminSection.tsx:145`).

**Maturity:** Partial. The request and approval path is close to Complete. Accrual and policy administration are API-level and have bugs.
**Tests:** `LeaveTypeServiceTests`, `LeavePolicyRuleServiceTests`, `LeaveRequestServiceTests`, `Leave/LeaveApprovalServiceTests` (384 lines) and `Leave/LeaveApprovalAccessServiceTests`. **No tests** for accrual, year-reset or import.

### 1.8 Tasks
**Exists**
- Task CRUD (Admin role for writes) with priority, start and due dates, and status (Assigned, InProgress, Blocked, Completed).
- Status PATCH is allowed for the assignee or an admin (`TasksController.cs:112-134`, `CanAccessTaskAsync`).
- A task-assigned notification is sent.
- The employee can start a task from the portal.
- UI: `/tasks` and `/tasks/calendar`.

**Gaps**
- No comments or attachments.
- No manager-created tasks: writes are Admin-only.

**Maturity:** Complete (simple scope).
**Tests:** none on the backend.

### 1.9 Manager / team
**Exists**
- `GET /api/Manager/team`: direct reports with summary counts (active, pending leave, attendance today, overdue tasks, upcoming scheduled changes). It is server-scoped to the manager's reporting tree unless the caller is an admin (`ManagerTeamAccessService.cs`).
- The leave approval queue (see 1.7).
- UI: `/manager/team` with filters, summary cards, table and approvals panel.

**Gaps**
- No team attendance or timesheet approval.
- No team roster view and no bulk actions.

**Maturity:** Complete for the dashboard. Partial for manager actions.
**Tests:** `Manager/ManagerTeamServiceTests.cs`, `Manager/ManagerTeamAccessServiceTests.cs`.

### 1.10 Employee portal / self-service
**Exists**
- Eligibility check (is the user linked to an employee record?).
- The portal shows the employee's schedule (shifts and tasks), leave balances and up to 50 leave requests (`EmployeePortalService.cs`).
- Start a shift or task, and self-service leave at `/employee-portal/leave`.
- It has its own `EmployeePortalShell`.

**Gaps**
- No self check-in or check-out: attendance is only on the admin page.
- No viewing or editing of own profile, no own documents, no payslips.
- `/employee-portal` is not in the RBAC menu seed. It is reached from the Home CTA (`ems-web/src/app/page.tsx:29`).

**Maturity:** Partial.
**Tests:** none.

### 1.11 RBAC / authorisation
**Exists**
- UM roles map to **role keys**.
- `RoleKeyPermissions` give menu-level allow/deny. `RoleKeyCapabilities` give feature capabilities, but only `employees.view|manage|access|export` are defined (`EmployeeCapabilities.cs`).
- Navigation is built from permitted menus, with dedupe (`NavigationService.cs`).
- The startup seed creates 22 menus and gives ADMIN all of them (`EMS.API/Bootstrap/EmsRbacSeedHostedService.cs:30-52`).
- Position-driven roles, plus per-employee direct overrides (`EmployeeRolesController.cs`).
- Admin diagnostics: `/api/authorization-cutover/readiness`, `/api/AuthorizationTelemetry`, `RoleKeyBackfillHostedService` and `RoleKeyMigrationHealthCheck`.
- UI: `/user-management/menu-access` (tri-state tree) and `/user-management/employee-capabilities`.

**Gaps / risks**
- **Enforcement is uneven.** Capability checks exist for Employees, plus Leave requests, balances and attachments, Manager, Tasks and Shifts. They are missing for Attendance, Departments, Sites, Locations, Job positions, Organisations, Documents, Leave admin operations and **Menus CRUD**. `MenusController.cs` is authenticate-only, so any user can change the menu tree.
- The frontend has no route-level guard. Menus only hide links (`ems-web/src/providers/ProtectedShell.tsx`).
- Menus are missing from the seed for `/employee-portal`, `/employee-transfers`, `/shifts/calendar` and `/tasks/calendar`.
- Cutover and telemetry endpoints have no UI.

**Maturity:** Partial.
**Tests:** `Employees/EmployeeAccessServiceTests.cs` and `Leave/LeaveApprovalAccessServiceTests.cs`. Nothing for `PermissionEvaluator` or the RoleKey services.

### 1.12 User management (Pukar.Usermanagement, in-process)
**Exists**
- Auth: register, login, refresh, revoke, forgot, reset and change password.
- JWT issue and validation, plus JWKS. BCrypt hashing. SMTP email (`SmtpEmailSender.cs`).
- Invitations and accept-invitation.
- Users and Roles CRUD, user roles, and internal service-token APIs.
- Idempotency cleanup job and an opt-in admin seed.
- UI: `/login`, `/forgot-password`, `/reset-password`, `/change-password`, `/accept-invitation`, `/user-management/users` and `/user-management/roles`.
- New users must change their password on first login.

**Risks**
- `/api/auth/register` is `[AllowAnonymous]` with no configuration gate (`AuthController.cs:22-24`), so anyone can self-register.
- No login lockout or rate limiting. Only the service-token endpoint is rate-limited (`ServiceAuthService.cs`).
- The initial password is predictable (`FirstName@123`, `docs/architecture.md` "Recent EMS changes").
- No MFA.

**Maturity:** Complete for basic auth.
**Tests:** `Pukar.Usermanagement.UnitTests` (5 files, 14 tests) and `Pukar.Usermanagement.IntegrationTests` (13 tests).

### 1.13 Notifications (Pukar.Notifications)
**Exists**
- A reusable core library (`Pukar.Notifications/*`), the `ntf.Notifications` table and the inbox API (list, unread count, mark read, mark all read).
- A header bell that polls every 60 seconds (`useUnreadNotificationCount.ts:13`).
- 7 event types (`EmsNotificationTypeKeys.cs`): task assigned, leave submitted, leave approved, leave rejected, scheduled change upcoming, invitation failed, document expiring.
- Sends are wrapped by `SafeNotifyAsync`.

**Gaps**
- **In-app only.** There is no email, push or SMS channel for these events, and no real-time push (polling only).
- No per-user preferences.

**Maturity:** Complete for in-app.
**Tests:** `Notifications/NotificationServiceTests.cs` and `EmsNotificationProducerTests.cs`.

### 1.14 Audit and history
**Exists**
- `AuditSaveChangesInterceptor` writes `AuditTrailEntry` rows (entity, key, action, old and new JSON, actor, correlation ID) for every tracked EF change (`EMS.Infrastructure/Persistence/Auditing/AuditSaveChangesInterceptor.cs`).
- Separate effective-dated employee history is exposed through `/api/EmployeeHistory/{id}`.

**Gaps**
- **No API or UI to read the audit trail.**
- No filtering of sensitive fields in old/new JSON.
- No audit retention or archiving.

**Maturity:** Partial (write-only).
**Tests:** none.

### 1.15 Reporting, exports and imports
- Exports: employee directory to CSV; attendance to CSV, XLSX or PDF.
- Imports: leave requests only (CSV text pasted into the UI).
- Home dashboard: employee and department counts only (`features/dashboard/components/HomeOverview.tsx`).
- Missing: leave liability or balance reports, headcount and turnover reports, a report builder, and employee or balance imports.
- **Maturity:** Partial.

### 1.16 Integrations and background jobs
- Hosted services (`EMS.API/Program.cs:94-103`): RBAC seed, role-key backfill, scheduled-change applier, scheduled-change reminders, document-expiry reminders, integration outbox dispatcher (EMS → UM provisioning, idempotent), UM seed, and UM idempotency cleanup (hourly).
- They are plain `BackgroundService` polling loops (60-second default, `WorkerPollIntervalSeconds`). There is no job framework and no distributed lock, so running several instances would duplicate work.
- No external integrations (payroll, Xero/MYOB/KeyPay, calendar, SSO). The HTTP UM gateway code still exists for a remote UM (`EMS.Infrastructure/Integrations/UserManagement/Http*`), while the in-process gateway is active.

### 1.17 Cross-cutting UX
- Global command palette that searches menus and employees and offers quick actions (`features/command-palette`, with 3 vitest files).
- A UI rule bans showing raw IDs (`.cursor/rules/ems-ui-no-id-display.mdc`).

### 1.18 Leftovers and stubs
- `WeatherForecastController.cs` and `WeatherForecast.cs` (template, still routable).
- `EMS.Infrastructure/Class1.cs`.
- `EMS.Application.UnitTests/UnitTest1.cs` (`Assert.Pass`).
- `EMS.API.IntegrationTests/EmployeeAccessIntegrationTests.cs` has 1 placeholder test (`Assert.That(true)`), so **there are no real EMS API integration tests**.
- `README.md` "Tests" section says no tests exist, which is out of date.

---

## 2. Deployment and operations
| Capability | State | Evidence |
|---|---|---|
| Docker Compose: SQL Server 2022, Mongo 7, Redis 7.2, mongo-express, ems-api, ems-web | Present (dev-oriented: `ASPNETCORE_ENVIRONMENT=Development`, mongo-express with `ME_CONFIG_BASICAUTH=false`) | `docker-compose.yml`, `EMS.API/Dockerfile(.compose)`, `ems-web/Dockerfile`, `start-ems-docker.bat` |
| Health checks: `/health` and `/health/ready` covering the EMS database, UM database, role-key migration and Mongo (optional) | Present | `EMS.API/Program.cs:209-279` |
| Logging: Serilog with request logging, Mongo sink (optional) and correlation-ID middleware | Present | `Program.cs:49-56,248-250`, `Middleware/CorrelationIdMiddleware.cs` |
| **Redis** | **Provisioned but not used.** No cache or `IDistributedCache` usage anywhere in the code, only a connection string | grep of `*.cs` |
| Secrets via `.env`, with required-variable guards in compose and production CORS validation | Present | `docker-compose.yml`, `Program.cs:60-81` |
| OpenAPI | Development only, no Swagger UI | `Program.cs:245` |
| CI/CD | **None** (no `.github/`, no pipeline files) | — |
| File storage | Local disk under `wwwroot/attachments`, so it does not scale horizontally and is publicly served (see 1.4) | `EMS.API/Services/Local*Storage.cs` |
| Migrations | Manual `dotnet ef`. A gated destructive migration `RemoveLegacyUserManagementTables` exists | `README.md` |

## 3. Multi-tenancy
- The schema is **organisation-ready**: most entities carry `OrganizationId`.
- The runtime is **single-tenant**: `OrganizationService.cs:24-25` throws "Only one organization is allowed per instance."
- Global, non-tenanted tables include `Site`, `DocumentType`, `Menu`, `RoleKeyPermission` and `RoleKeyCapability`. `EmployeeRetentionPolicy.OrganizationId` is nullable.
- There is no tenant resolution from the token, and no EF global query filters. Moving to multi-tenant would need tenant claims, query filters, and organisation columns on the global tables.

## 4. Mobile readiness
- Responsive Tailwind layouts and a separate employee-portal shell. Check-in captures browser geolocation.
- No PWA: no manifest or service worker in `ems-web/public`, no offline mode, no push.
- No native app.
- The JWT API is reusable by a mobile client, but the mobile-critical flows are missing from the portal: self check-in, notifications push, profile.
- **Readiness: low to moderate.**

## 5. Australian compliance readiness
| Area | State |
|---|---|
| NES leave types (annual, personal/carer's, compassionate, family and domestic violence, long service, community service, parental) | **Not seeded.** Leave types are free-form per organisation. No LSL state rules |
| Accrual | Generic rate and frequency, with proration by calendar days. No accrual by ordinary hours worked (the NES basis for annual and personal leave), no casual exclusion (no employment-type field), and no 38-hour-week basis. Year-reset carry-forward bug and non-idempotent accrual (see 1.7) |
| Public holidays and working days | None. Leave amounts are entered by the user |
| Awards, pay rates, overtime and penalties | None |
| Employee records (Fair Work Regulations 3.31–3.44: pay, hours, leave, super records, kept 7 years) | Partial. There are leave and attendance records, but no pay or super data. The retention policy is generic `RetentionDays` (an organisation setting, not a 7-year default), and **no purge or anonymise job** acts on `RetentionUntilUtc` |
| Privacy Act / APPs (personal information security) | Risks: documents and attachments can be served anonymously, document and attendance endpoints have no access checks, and the audit log stores full old/new JSON |
| Time zone | `Australia/Sydney` default for business dates only. Attendance and shifts are stored in UTC, with no per-site time zone |
| Spelling | The code and UI use US "organization" (identifiers); the UI copy is mixed |

**Overall: Low.** EMS is a sound HR-records and workflow base. It is not yet a compliant AU leave and time system.

---

## 6. Summary matrix
| Module | UI | Maturity | Backend tests |
|---|---|---|---|
| Employees | Full | Complete (core) | Strong (10 files) |
| Org structure | Full except Locations and site assignment | Partial | None |
| Onboarding | Full | Complete | Yes |
| Documents | Full (on profile) | Partial (security) | None (FE only) |
| Attendance | Full | Partial (no authorisation) | None |
| Shifts | Full | Partial | None (FE only) |
| Leave | Mostly; policy and accrual are API-only | Partial (bugs) | Good for requests and approval; none for accrual or import |
| Tasks | Full | Complete | None |
| Manager / team | Full | Complete / Partial | Yes |
| Employee portal | Full | Partial | None |
| RBAC | Full admin UI | Partial (uneven enforcement) | Limited |
| User management | Full | Complete (basic) | Yes (UM projects) |
| Notifications | Full | Complete (in-app) | Yes |
| Audit | None | Partial (write-only) | None |
| Reporting / exports | Partial | Partial | None |

## 7. Top findings for strategy
1. **Security and authorisation gaps are the biggest risk.** Many controllers have no authorisation beyond sign-in: Attendance, Documents, Menus, Leave operations, and the org-structure controllers. Uploaded files are publicly served. Anonymous self-registration is open.
2. **Leave balance defects**: year-reset double-counts carry-forward, and accrual is not idempotent. Both fall in a DEV-ORG high-risk area (leave balances).
3. **API-only features need UI or a schedule**: policy rules, accrual and year-reset, Locations, site assignment, the audit trail, and cutover/telemetry.
4. **The test pyramid is uneven.** There are 93 EMS unit tests, focused on Employees, Leave and Manager. EMS API integration tests are zero (placeholder only), and there is no CI.
5. **Docs are out of date**: the leave docs say the workflow is deferred, and the README says there are no tests.
6. **Redis is unused.** It is either a removal candidate or a planned caching layer.
