# EMS Build Backlog

Run: 20261115-2230-EMS-strategy · Owner: Product Owner · Created: 2026-11-15
Source: `05-priorities.md` (High priority tier, plus the first competitiveness items). EMS is a personal portfolio project.

## Format rules (for the scheduled agent)
- One task per `## Bxx — Title` heading. Work tasks in ID order; start a task only when every task in its `Depends-on` line is `Done`.
- The `Status:` line is the only line the agent changes. Allowed values: `Todo`, `In progress`, `Done`, `Blocked`.
- When setting `Blocked`, add one line directly below it: `Blocked-reason: <plain-language reason>`. Remove that line when unblocked.
- When setting `Done`, add one line directly below it: `Done-note: <date> <run id or branch>`.
- CEO standing decision (2026-11-15): all gates are approved for scheduled runs, including `Risk: High` tasks. Risk levels are still recorded and reported in the weekly email.
- Every task must leave the build and test check (B01) green.

---

## B01 — Automatic build and test check, and tidy leftovers
Status: Done
Done-note: 2026-11-17 feature/b01-ci-build-check
Risk: Medium
Areas: backend, frontend, ci
Depends-on: none
Sources: C8, C17, C20
Goal: Add a GitHub Actions workflow that, on every push and pull request, builds the .NET solution, runs all .NET tests, and runs the frontend lint, tests and production build. Make it pass: fix any compile errors left by the uncompiled manager leave-approval feature (except migration problems, which are B02), fix the architecture-test breach in the Departments controller by handling the conflict in the service, and remove template leftovers (WeatherForecast, Class1, UnitTest1, the placeholder integration test, the `.csproj.user` file) and add `artifacts/` to `.gitignore`.
Acceptance:
- The workflow runs on push and pull request and shows a green result on the branch.
- All existing .NET tests and architecture tests run and pass; no placeholder tests remain.
- Frontend lint, tests and build pass in the workflow.
- The Departments controller no longer references EF Core; duplicate-name conflicts still return the same response.
- WeatherForecast endpoint no longer exists.
- If the repository is not on GitHub with Actions enabled, set Blocked and say so.

## B02 — Repair and verify the manager leave-approval migration
Status: Done
Done-note: 2026-11-18 feature/b02-leave-approval-migration
Risk: High
Areas: backend, migration
Depends-on: B01
Sources: C17
Goal: The last feature's database migration and model snapshot were written by hand and never compiled. Regenerate or correct them with the EF tools so the migration has its designer file, the snapshot matches the model, and there are no pending model changes. No change to the intended database shape.
Acceptance:
- The migration `AddLeaveRequestDecisionFields` has a matching designer file.
- The EF "pending model changes" check reports none, and is added to the B01 workflow.
- Applying all migrations to an empty database succeeds in the workflow (or locally, with evidence).
- The decision fields (reviewer, reviewed time, rejection reason) are unchanged from the intended design.

## B03 — Integration test harness for "who can do what"
Status: Done
Done-note: 2026-11-19 feature/b03-access-test-harness
Risk: Low
Areas: backend
Depends-on: B01
Sources: C8
Goal: Create real API integration tests that start the API in memory with a test database and sign in as different kinds of users (no role, ordinary employee, manager, admin). Add a simple reusable way to write "this user calling this endpoint gets this result" tests, so every security task after this one adds its own cases. Start with endpoints that are already protected correctly.
Acceptance:
- Tests run in the B01 workflow with no external services needed beyond what the workflow provides.
- Helper lets a test sign in as a user with chosen roles and an optional linked employee.
- At least five cases pass for already-protected endpoints (for example role-key permissions admin-only, an employee reading their own portal).
- A short note in the test project explains how to add a case.

## B04 — Lock role assignment and admin-only setup endpoints
Status: Done
Done-note: 2026-11-20 feature/b04-lock-admin-endpoints
Risk: High
Areas: backend
Depends-on: B03
Sources: C2, C1
Goal: Stop ordinary users changing who is an admin or how the organisation is set up. Require admin access for changing job-position roles, and for creating, changing or deleting organisations, departments, sites, locations, job positions, menus, leave types, leave policy rules, the leave accrual and year-reset runs, and leave imports. Use one named policy per area (for example "organisation admin", "leave admin") that maps to the admin role for now, so finer roles can be added later. Reading lists needed for dropdowns stays open to signed-in users.
Acceptance:
- A non-admin changing a job position's roles gets 403, and no roles are synced to employees.
- A non-admin gets 403 on every create, change and delete action listed above; an admin succeeds.
- Signed-in users can still read departments, positions, locations and leave types used by forms.
- Integration tests cover each controller listed (at least one denied and one allowed case each).

## B05 — Close public self-registration and slow down password guessing
Status: Done
Done-note: 2026-11-21 feature/b05-close-registration
Risk: High
Areas: backend, frontend
Depends-on: B03
Sources: C1, C7
Goal: Turn off anonymous self-registration by default behind a setting, since staff join through invitations. Add rate limiting to the sign-in, registration, refresh, forgot-password and reset-password endpoints, per address and per email, returning a clear "too many attempts, try again shortly" message that the login screen shows.
Acceptance:
- With default settings, the register endpoint returns 404 or 403 and creates no user.
- Invitation acceptance still works.
- Repeated sign-in attempts beyond the limit return 429 with a retry time; the login page shows a friendly message.
- Forgot-password is limited per email so it cannot flood an inbox.
- Integration tests cover registration off, and the rate limit triggering.

## B06 — Fix the Leave permission leak
Status: Done
Done-note: 2026-11-22 feature/b06-leave-permission-leak
Risk: High
Areas: backend
Depends-on: B03
Sources: C5
Goal: Having the ordinary Leave page currently lets staff view balances, create or cancel requests and download medical certificates for any employee. Change the check so only leave admins can act on other employees' leave, managers can see their direct reports' leave (reusing the existing approval scoping), and everyone else can act only on their own.
Acceptance:
- (Reviewer 2026-11-20) The leave approval service treats any role with the `leave.admin` menu as a leave admin while the `LeaveAdmin` policy is admin-only; make both use the same definition.
- An employee with only the Leave page is denied another employee's balances, requests and attachments.
- A leave admin is allowed.
- A manager can view a direct report's requests and balances but not those of someone outside their team.
- Employees still see and manage their own leave as before.
- Unit and integration tests cover all four cases.

## B07 — Make attendance safe
Status: Done
Done-note: 2026-11-23 feature/b07-safe-attendance
Risk: High
Areas: backend, frontend
Depends-on: B03
Sources: C1
Goal: Anyone can currently clock in, clock out or enter manual time for anyone. Self actions (check-in, check-out, breaks) must work out the employee from the signed-in user. Acting for someone else, manual entries and organisation-wide reports and exports need an attendance-admin policy; managers may view their direct reports' attendance. Keep the admin attendance page working for admins.
Acceptance:
- An ordinary employee can check in and out only for themselves, whatever employee ID is sent.
- Manual entries and changes for another employee need attendance admin; others get 403.
- Reports and exports are limited to the caller's own records, their direct reports (managers) or everyone (admins).
- The admin attendance page still works for an admin.
- Integration tests cover self, other-employee, manager and admin cases.

## B08 — Private storage and authorised downloads for documents and leave attachments
Status: Done
Done-note: 2026-11-24 feature/b08-private-file-storage
Risk: High
Areas: backend, frontend
Depends-on: B04
Sources: C3
Goal: Employee documents and leave attachments are stored in the public web folder and can be downloaded without logging in. Move them to a private storage folder set in configuration, serve them only through the existing authorised download endpoints, and stop returning storage paths to the browser (return a download link instead). Include a one-off, repeatable step that moves existing files. The organisation logo stays public.
Acceptance:
- Requesting an old public attachment URL without signing in returns 404.
- Downloads work through the authorised endpoints for permitted users.
- Response models no longer include storage paths.
- A documented, repeatable step moves existing files; running it twice does no harm.
- The attachment storage rule file is updated to separate public and private files.

## B09 — Access checks and paging for documents
Status: Todo
Risk: High
Areas: backend, frontend
Depends-on: B08
Sources: C1, C16
Goal: Any signed-in user can list or download any employee's documents. Add access checks using the existing employee access rules: employees see their own documents, managers their direct reports', and document admins everyone's. Listing all documents requires admin, and lists are paged.
Acceptance:
- An employee cannot list, download, change or delete another employee's documents (403).
- A manager can view direct reports' documents only.
- An admin can list all documents, with paging.
- The employee profile documents section still works.
- Integration tests cover the four cases.

## B10 — Safe upload rules
Status: Todo
Risk: High
Areas: backend
Depends-on: B08
Sources: C6
Goal: Uploads currently accept dangerous file types and keep the uploaded file extension. Create one shared upload checker used by documents, leave attachments and the logo: allow only listed types, check the extension, the declared type and the file's actual content signature together, and save with an extension the server chooses. Drop SVG for logos. Downloads are sent as attachments with "do not guess the type" protection.
Acceptance:
- (Reviewer 2026-11-24) User-supplied file names and extensions no longer reach the stored file key or the download Content-Disposition unsanitised; add nosniff on downloads.
- An HTML file renamed or declared as PDF is rejected.
- Valid PDF, Word and image files are accepted and saved with the server's extension.
- SVG logos are rejected; PNG and JPEG are accepted.
- Downloads carry attachment and no-sniff headers.
- Unit tests cover each allowed type and at least three disguised files.

## B11 — Fix the leave-year reset double count
Status: Todo
Risk: High
Areas: backend
Depends-on: B04
Sources: D1
Goal: After a leave-year reset, the carried-forward amount is counted twice, so available leave is doubled. Correct the reset so the carried amount is counted once, matching the available-balance formula, and add tests for reset with and without a carry-forward limit.
Acceptance:
- After a reset, available balance equals the capped carried-forward amount, not twice it.
- The carry-forward limit is respected.
- Unit tests cover: no limit, a limit lower than the balance, and a zero balance.
- A note explains how to check and correct any balances affected in an existing database (no automatic data change).

## B12 — Accrual can run only once per period; protect balances from collisions
Status: Todo
Risk: High
Areas: backend, migration
Depends-on: B11
Sources: C4, C16
Goal: Each accrual run adds leave again, even for a period already accrued. Record each completed run by organisation, leave type and period, unique per period, so a repeat run adds nothing and says so. Add a concurrency check to leave balances so an approval and an accrual at the same moment cannot overwrite each other. Load balances in one query instead of one per employee.
Acceptance:
- Running accrual twice for the same period changes balances only once; the second run reports "already accrued".
- Simultaneous updates to the same balance produce a clear conflict, not a lost update.
- New migration adds the run record and the concurrency token; it applies cleanly in the workflow.
- Accrual for a new period still works.
- Unit tests cover first run, repeat run and a concurrency conflict.

## B13 — Account lockout after repeated failed sign-ins
Status: Todo
Risk: High
Areas: backend, migration, frontend
Depends-on: B05
Sources: C7
Goal: Add progressive lockout in User Management: after a set number of failed sign-ins, the account is locked for a short, growing period. A successful sign-in or password reset clears the count. The login screen explains the lockout without revealing whether the email exists.
Acceptance:
- (Reviewer 2026-11-21) Per-email sign-in and forgot-password limits count only failed attempts, keyed per email and address, so nobody can keep a known user (e.g. the admin) locked out by spamming their email.
- (Reviewer 2026-11-21) Sign-in and forgot-password take the same time whether or not the email exists (dummy password check for unknown users; reset email sent in the background).
- After the configured number of failures, sign-in is refused for the lockout period even with the right password.
- The counter resets after a successful sign-in or password reset.
- The message is the same whether or not the email exists.
- Migration adds the failure count and lockout time to users; it applies cleanly.
- Unit and integration tests cover lockout, expiry and reset.

## B14 — Secrets out of the repository; Docker that works with one command
Status: Todo
Risk: High
Areas: backend, docker
Depends-on: B01
Sources: C9, C10, C21
Goal: Remove the committed database password, signing key and default admin password; provide an example settings file and use user secrets or the `.env` file instead. The API refuses to start if the signing key is the example value. Fix the Docker build so it includes every project it needs, use a Docker-specific environment whose seed admin password comes from `.env`, wait for the database to be healthy, keep database ports local-only and put the Mongo admin tool behind an optional profile with a password.
Acceptance:
- (Reviewer 2026-11-24) If the container runs as non-root, the image creates and owns the private-files volume folder; document backing up the `ems-private-files` volume; add a logo volume; log a warning at start-up in Production if old public files exist and the move step is off.
- No real secrets remain in tracked settings files; an example file documents each value.
- The API refuses to start with the example signing key.
- `docker compose up` from a clean clone with a filled-in `.env` builds and starts the API and web app.
- No known default admin password is used in the Docker path.
- The workflow builds the Docker images.

## B15 — Frontend security upgrades and automatic scanning
Status: Todo
Risk: Medium
Areas: frontend, ci
Depends-on: B01
Sources: C15, C10
Goal: Upgrade the vulnerable direct frontend packages (Next.js, axios, zod), move build-only tools (shadcn CLI, SVG loader) to development dependencies, turn on Dependabot for npm, NuGet and GitHub Actions, and add a secret scan (Gitleaks) to the workflow.
Acceptance:
- The production dependency audit shows no critical or high issues in direct packages (any remaining are listed with a reason).
- Frontend lint, tests and build still pass.
- Dependabot configuration is present for npm, NuGet and Actions.
- The secret scan runs in the workflow and passes.

## B16 — Honest docs and README refresh
Status: Todo
Risk: Low
Areas: docs, frontend
Depends-on: B14
Sources: C11, C13, C19, C23, D5, D7
Goal: Bring the docs in line with the code. Rewrite the top of the README as a clear summary of a personal portfolio project, with a feature list, "run in a few minutes" using the fixed Docker setup, a build-status badge, and a short "design decisions" section that honestly explains trade-offs (including EF Core in the application layer). Align the coding rules, DEV-ORG version line and leave docs; remove Redis from the stack claims; mark retired documents as historical; check and correct the first-password description. Fix the two screens that show raw IDs to show names.
Acceptance:
- (Reviewer 2026-11-24) Refresh docs/dev-org/02-system-inventory.md for private storage and the changes since B01.
- README states it is a personal portfolio project and matches the current single-host design.
- No document claims Redis is used; DEV-ORG shows net9.0.
- Leave docs no longer say approval is deferred.
- The first-password description matches the code.
- Transfer forms show the current department or position name, or "Not assigned".
- Placeholders are left for screenshots for the CEO to add.

## B17 — Leave ledger foundation
Status: Todo
Risk: High
Areas: backend, migration
Depends-on: B12
Sources: Idea 8, C4, D1
Goal: Record every change to a leave balance as a dated entry: opening balance, accrual for a period, leave taken on approval, refund on cancellation, carry-forward and reset. Each entry says what caused it and the rule and inputs used. For now the stored balance is kept and updated alongside the entries, and a check confirms both agree. Existing balances get a single "opening" entry so the history starts from today.
Acceptance:
- Approval, cancellation, accrual and year reset each write a ledger entry in the same save as the balance change.
- The sum of entries equals the stored balance for every employee and leave type; a check reports any difference.
- A repeatable backfill creates opening entries for existing balances and does nothing if run twice.
- Migration applies cleanly in the workflow.
- Unit tests cover each entry type.

## B18 — "Why is my balance this number?" view
Status: Todo
Risk: Medium
Areas: backend, frontend
Depends-on: B17
Sources: Idea 8
Goal: Show employees, their managers and leave admins a plain-English list of the entries that make up a leave balance, for example "+2.92 hours accrued for 1–31 October at 38 hours a week". Employees see only their own; access follows the B06 rules.
Acceptance:
- The employee portal leave page has a "Why this balance?" view per leave type.
- Each line shows date, plain-English description and amount, with a running total that matches the balance.
- Managers and leave admins can open it from the employee's leave details; others get 403.
- No raw IDs are shown.
- Tests cover access rules and the running total.

## B19 — Balance adjustments and opening balances with reasons
Status: Todo
Risk: High
Areas: backend, frontend
Depends-on: B18
Sources: Idea 8, inventory 1.7 gap
Goal: Today a balance can only change through accrual runs. Let leave admins set an opening balance or make an adjustment, always with a written reason, recorded as a ledger entry and in the audit trail. Adjustments cannot be edited or deleted; a mistake is fixed with a reversing adjustment.
Acceptance:
- Leave admins can add an opening balance or adjustment with a required reason; others get 403.
- The entry appears in the "Why this balance?" view with the reason.
- Entries cannot be edited or deleted; a reversal entry is offered instead.
- The audit trail records who made the adjustment.
- Unit and integration tests cover permission, required reason and reversal.

## B20 — Compliance health check, first set of checks
Status: Todo
Risk: Low
Areas: backend, frontend
Depends-on: B12
Sources: Idea 7
Goal: Add an admin page listing plain-English checks using data EMS already holds, each with a count, a short "why this matters" note and a link to fix it. First set: employees with no manager, documents expiring within 30 days, leave accrual not run for the last completed period, leave requests pending more than 5 days, employees with no linked login, and overdue onboarding tasks. Read-only, no score.
Acceptance:
- Only admins can open the page (403 for others).
- Each check shows a count and links to the screen that fixes it.
- Each check has a one-sentence explanation in plain English.
- Counts are correct against seeded test data (unit tests per check).
- Adding a new check needs only one new class plus its registration.

## B21 — Upgrade from .NET 9 to .NET 10 (LTS)
Status: Todo
Risk: High
Areas: backend, ci, docker
Depends-on: B02
Sources: Reviewer 2026-11-17 (B01 review)
Goal: .NET 9 left Microsoft support on 10 November 2026, so EMS no longer gets security fixes. Move every project, the CI workflow and the Dockerfiles to .NET 10 (the current long-term-support release), update packages that must move with it (EF Core, ASP.NET Core), and keep every test green.
Acceptance:
- All projects target net10.0; CI uses the .NET 10 SDK and passes.
- EF Core and ASP.NET Core packages are on matching 10.x versions; no pending model changes.
- Dockerfiles use .NET 10 images.
- Docs that name the .NET version are updated.

## B22 — Move the frontend and CI from Node 20 to a supported Node version
Status: Todo
Risk: Low
Areas: frontend, ci, docker
Depends-on: B01
Sources: Reviewer 2026-11-17 (B01 review)
Goal: Node 20 reached end of life in April 2026. Move CI, the ems-web Dockerfile, the package.json engines field and CONTRIBUTING.md to Node 22 or 24 LTS, keeping lint, tests and build green.
Acceptance:
- CI runs the frontend on the new Node version and passes.
- ems-web Dockerfile and `engines` match it.
- CONTRIBUTING.md names the new version.

## B23 — CI check that every migration has its Designer file
Status: Todo
Risk: Low
Areas: ci, migration
Depends-on: B02
Sources: Reviewer 2026-11-18 (B02 review)
Goal: EF Core silently skips a migration that has no Designer file or no `[Migration]` attribute, and the current migration-history check counts those same attributes, so CI would not catch the exact mistake B02 fixed. Add a CI step that fails, with a clear annotation, if any migration `.cs` file has no matching `.Designer.cs` with a `[Migration]` attribute.
Acceptance:
- CI fails with an annotation naming the file when a migration has no Designer or no attribute (proved on a throwaway branch).
- CI passes on development as it stands.

## B24 — Keep the EF tools version in one place
Status: Todo
Risk: Low
Areas: ci, backend
Depends-on: B02
Sources: Reviewer 2026-11-18 (B02 review)
Goal: The dotnet-ef version is set separately in the CI workflow from the SDK and about 20 EF package references, so the .NET 10 upgrade (B21) could leave them out of step. Add a local tool manifest (`.config/dotnet-tools.json`) used by CI and developers, and note in B21 that the SDK, EF packages and tool manifest move together.
Acceptance:
- CI restores dotnet-ef from the tool manifest, not a hard-coded global install, and stays green.
- README explains `dotnet tool restore`.

## B25 — Fix the web-access (CORS) settings so EMS starts outside Development
Status: Todo
Risk: High
Areas: backend, docker
Depends-on: B03
Sources: Developer, Tester and Reviewer 2026-11-19 (B03)
Goal: Program.cs reads the allowed web origins from the wrong setting name (`Cors` as a list) while appsettings and docker-compose use `Cors:AllowedOrigins`. Outside Development no origins are found and the API stops at start-up, so the Production Docker image cannot run, and docker-compose's `EMS_CORS_ORIGIN_*` values are ignored. Read the origins from `Cors:AllowedOrigins` (as the User Management host does), keep the "no origins outside Development = refuse to start" safety rule, and remove the test-only `Cors:0` workaround from the integration test host.
Acceptance:
- An integration test starts the API as Production with `Cors:AllowedOrigins` set and it starts; with none set it still refuses to start.
- docker-compose origin variables are honoured.
- The test-only `Cors:0` setting is gone and all tests stay green.

## B26 — Test harness: request bodies and a temporary file area
Status: Todo
Risk: Low
Areas: backend
Depends-on: B03
Sources: Reviewer 2026-11-19 (B03 review)
Goal: The "who can do what" tests can only send requests without a body, and the test API writes into the source tree's `EMS.API/wwwroot`. Add support for JSON and file-upload bodies in access cases, and point the test API at a temporary folder that is cleaned up, before the document and upload tasks (B08–B10). If B04 already adds body support, only the file area remains.
Acceptance:
- (2026-11-24) The temporary file area part was delivered in B08 (each test factory gets temporary web and private folders); only request-body extras remain if any.
- An access case can send a JSON body and a file upload.
- Tests never write inside the repository folders.

## B27 — Optional SQL Server test database for checks InMemory can't make
Status: Todo
Risk: Low
Areas: backend, ci
Depends-on: B03
Sources: Reviewer 2026-11-19 (B03 review)
Goal: The in-memory test database ignores case-insensitive matching, foreign keys and the clash protection (row versions), so tests of sign-in lockout, accrual and balance protection could pass falsely. Add an opt-in fixture that runs selected integration tests against the SQL Server container in CI, and list the InMemory limits in the test README. Should be done before B12.
Acceptance:
- Tests marked for SQL Server run in CI against a real SQL Server and pass.
- A test proves a concurrent update is rejected on SQL Server.
- README lists the InMemory limits and when to use the SQL Server fixture.

## B28 — Only admins can grant roles (close the remaining self-promotion routes)
Status: Todo
Risk: High
Areas: backend
Depends-on: B04
Sources: Reviewer 2026-11-20 (B04 review)
Goal: B04 locked job-position roles, but a user with the ordinary `employees.access` capability can still give anyone, including themselves, the ADMIN role through `PUT employees/{id}/roles/direct-overrides` and `PUT employees/{id}/linked-user/roles`, and a user with `employees.manage` can transfer themselves into a position that grants ADMIN. Add an admin-only guard in the services that grant or change roles (direct overrides, linked-user roles, and position changes that would add ADMIN), so the rule holds however the request arrives.
Acceptance:
- (Reviewer 2026-11-22) A non-admin cannot grant any role that carries the `leave.admin` menu (or other admin-level menus) through these routes either — since B06 such a role unlocks all leave, medical certificates, accrual and year reset.
- A non-admin with `employees.access` gets 403 when granting ADMIN via direct overrides or linked-user roles; nothing changes.
- A non-admin with `employees.manage` cannot move anyone (including themselves) into a position that grants ADMIN.
- Admins can still do all three; "who can do what" tests cover each route.

## B29 — Stop the site staff list leaking personal details
Status: Todo
Risk: High
Areas: backend
Depends-on: B04
Sources: Reviewer 2026-11-20 (B04 review)
Goal: `GET api/Sites/{id}/employees` returns date of birth, phone, email and archive reason to any signed-in user, without the `employees.view` check used elsewhere. Apply the same employee-visibility rules (or return only names and positions to users without `employees.view`).
Acceptance:
- A plain employee gets either 403 or a list without personal details; an admin or `employees.view` holder sees what they see today.
- Integration tests cover both.

## B30 — One definition of "admin" across EMS
Status: Todo
Risk: High
Areas: backend
Depends-on: B04
Sources: Reviewer 2026-11-20 (B04 review)
Goal: EMS decides "is this user an admin" in three ways: the new named policies, `[Authorize(Roles = Admin)]` attributes (case-sensitive) and a service-level ADMIN/ADMINISTRATOR list. A user with the "Administrator" role passes some checks and fails others. Make every check use the one shared admin rule and switch the remaining attributes to named policies.
Acceptance:
- A single shared admin check is used by every policy, attribute and service.
- Tests show ADMIN and ADMINISTRATOR users get the same results everywhere.

## B31 — Friendly "not allowed" messages and a safe /setup page
Status: Todo
Risk: Low
Areas: frontend, backend
Depends-on: B04
Sources: Reviewer 2026-11-20 (B04 review)
Goal: When the API refuses an action the web app shows "Request failed with status code 403", and `/setup` sends non-admins to a form that now always fails. Show a plain message ("You don't have permission to do this. Ask an administrator.") for 403 responses, and only show the setup flow to admins.
Acceptance:
- Any 403 shows the friendly message.
- Non-admins never see the /setup form.

## B32 — Correct client addresses behind a proxy, and IPv6-aware limits
Status: Todo
Risk: High
Areas: backend, docker
Depends-on: B05
Sources: Reviewer 2026-11-21 (B05 review)
Goal: The sign-in rate limits key on the connection address. Behind Docker Desktop, the Next.js proxy or a reverse proxy, every user looks like the same address (or "unknown"), so one attacker could block sign-in for everyone with about 21 requests a minute. Separately, each IPv6 address gets its own bucket, so an attacker with a /64 range can bypass per-address limits. Add trusted forwarded-header handling with known proxies/networks from configuration (never trust forwarded headers by default), group IPv6 addresses by /64 and map IPv4-mapped addresses, and log a warning when the address is unknown. Do with or before B14.
Acceptance:
- With a configured trusted proxy, limits key on the real client address; without one, forwarded headers are ignored.
- Two IPv6 addresses in the same /64 share a bucket.
- Integration tests cover both, and the Docker/compose setup documents the proxy setting.

## B33 — Keep leave admins inside their own organisation
Status: Todo
Risk: High
Areas: backend
Depends-on: B06
Sources: Reviewer 2026-11-22 (B06 review)
Goal: Since B06 a role given the `leave.admin` menu counts as a leave admin, but nothing limits that person to their own organisation: they can run accrual or year reset, edit leave types and see the summary and any employee's leave across organisations. Scope leave-admin actions to the caller's linked employee's organisation (full admins keep cross-organisation access).
Acceptance:
- (Reviewer 2026-11-23) The same organisation limit applies to attendance admins (roles given the `attendance.admin` menu): manual entries, acting for others, reports and exports stay within their own organisation.
- A non-admin leave admin gets 403 for another organisation's leave operations, types, summary and employees' leave.
- Within their own organisation everything works as today; admins are unaffected.
- Integration tests cover both.

## B34 — Cancelling leave already taken needs approval
Status: Todo
Risk: High
Areas: backend, frontend
Depends-on: B06
Sources: Reviewer 2026-11-22 (B06 review)
Goal: An employee can cancel approved leave after it has started or finished and get the days back without anyone approving it. Only allow self-cancellation of approved leave before it starts; after that, cancellation must be done or approved by a leave admin or the manager, with a reason. Fits alongside the leave ledger (B17, B19).
Acceptance:
- An employee cancelling approved leave that has started gets a clear refusal; before the start date it still works.
- A leave admin can cancel with a reason and the balance is restored once.
- Tests cover before, during and after the leave dates.

## B35 — Fill leave access test gaps
Status: Todo
Risk: Low
Areas: backend
Depends-on: B06
Sources: Tester 2026-11-22 (B06 test report)
Goal: Add the missing end-to-end tests: a manager of an archived employee is denied; the manager approvals queue shows only direct reports; a leave admin can reject, run accrual and year reset; a user with no role is refused the admin summary.
Acceptance:
- Each case above has an integration test that passes in CI.

## B36 — Attendance dates and lateness in the organisation's time zone
Status: Todo
Risk: High
Areas: backend, frontend
Depends-on: B07
Sources: Reviewer 2026-11-23 (B07 review)
Goal: Attendance work dates and the 'late' rule use UTC, so a morning check-in in Sydney is recorded on the previous day and 'late after 09:15' is measured in UTC, so effectively nobody is ever late. Store an organisation time zone (default Australia/Sydney), compute work dates, lateness and daily/period summaries in that zone, and show local times in the app.
Acceptance:
- A 08:30 Sydney check-in is dated that Sydney day and is on time; 09:30 is late.
- Summaries and exports group by local date.
- Daylight-saving changes are handled (tests on both sides of the change).

## B37 — Only one open attendance session per person
Status: Todo
Risk: Medium
Areas: backend, migration
Depends-on: B27
Sources: Reviewer 2026-11-23 (B07 review)
Goal: Two check-ins sent at the same moment can create two open sessions for one person. Add a database rule (filtered unique index on employee + open session) and turn the clash into a clear 'already checked in' response.
Acceptance:
- Two simultaneous check-ins create exactly one open session (proved on the SQL Server test database from B27).
- The second caller gets a clear 409 message.

## B38 — Protect check-in location data
Status: Todo
Risk: Medium
Areas: backend, frontend
Depends-on: B07
Sources: Reviewer 2026-11-23 (B07 review)
Goal: Managers currently see the precise GPS position of their reports' check-ins, and when an admin clocks someone in, the admin's own location is saved against that person. Do not record a location for actions done on someone else's behalf, and show managers only whether a location was captured (or a coarse area), keeping precise coordinates for attendance admins.
Acceptance:
- Admin-on-behalf actions store no location.
- Managers' views and exports contain no precise coordinates; attendance admins still see them.
- Tests cover each role.

## B39 — Record who changed attendance times
Status: Todo
Risk: Medium
Areas: backend, frontend
Depends-on: B07
Sources: Reviewer 2026-11-23 (B07 review)
Goal: When an attendance admin sets or corrects a time (manual entry or clocking someone in or out), there is no visible record of who did it or why, and no sensible limits. Require a reason for admin-set times, record the admin and reason on the record, limit how far back or forward a time can be set, and show 'entered by' in the admin views.
Acceptance:
- Admin-set times require a reason and store who set them.
- Times outside the allowed window are rejected with a clear message.
- Admin views and exports show who entered the time.

## B40 — Attendance exports show names, not raw IDs
Status: Todo
Risk: Low
Areas: backend
Depends-on: B07
Sources: Reviewer 2026-11-23 (B07 review)
Goal: PDF and Excel attendance exports show an EmployeeId column and no names, which goes against the project rule of never showing raw database IDs to people. Show employee number and name instead.
Acceptance:
- Exports show employee number and name; no raw database ID column.
- Export tests updated.
