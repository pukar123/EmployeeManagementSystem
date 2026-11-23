# EMS.API.IntegrationTests — "who can do what"

These tests start the real EMS.API in memory and call its endpoints as different kinds of signed-in user. Each case reads "this user, calling this endpoint, gets this status". They exist so that a later change cannot quietly loosen access without CI going red.

EMS is a personal portfolio project.

## What is real and what is swapped

Real (production registrations): token issuing (`IJwtTokenService`), bearer token validation, the global "must be signed in" rule, the admin policies (`AdminAccess`, `RoleAdmin`, `OrganizationAdmin` and `MenuAdmin` mean "is an admin"; `LeaveAdmin` means "admin or `leave.admin` menu" since B06; `AttendanceAdmin` means "admin or `attendance.admin` menu" since B07), and every EMS access service (linked employee, leave admin, attendance admin, menu and capability checks, manager scope).

Swapped for tests only (`Harness/EmsApiFactory.cs`):

- both databases use EF Core InMemory, with a new database for each test fixture;
- background and seed hosted services are removed (the harness runs the RBAC seed itself);
- Mongo logging is turned off and email goes to a no-op sender;
- the host runs in the `Testing` environment with a test-only signing key;
- the real role sync service (`IEmployeeRoleSyncService`) is wrapped, not replaced, by `RecordingEmployeeRoleSyncService`, which notes every call in `Factory.RoleSyncs` and then runs the real service;
- a test-only startup filter (`Harness/TestClientIpStartupFilter.cs`) sets the client IP from the `X-Test-Client-Ip` request header, so rate-limit tests can act as several clients. TestServer otherwise gives every request no IP (one shared `"unknown"` bucket). Production code never reads this header, nor `X-Forwarded-For`.

The auth rate limits (`AuthRateLimiting`) are on, with production defaults, in every fixture. The access fixtures mint tokens in-process and never call `/api/auth/*`, so they never reach a limit.

## Fixtures

| Fixture | What it covers |
|---|---|
| `Access/WhoCanDoWhatTests.cs` | Read-only "who can do what" cases on the shared `TestSeed` (B03). |
| `Access/AdminSetupAccessTests.cs` | B04: role assignment and setup writes are admin-only, their reads stay open. Adds its own rows (`Access/AdminSetupSeed.cs`), so its writes never touch the shared seed. |
| `Access/LeaveAccessTests.cs` | B06: leave requests, balances and attachments are reachable only by the employee, their current direct manager (read and approve only, no attachments) and leave admins (admin or `leave.admin` menu); the `LeaveAdmin` policy and the admin summary follow the same rule. Adds its own rows and attachment files (`Access/LeaveAccessSeed.cs`) and removes the files afterwards. |
| `Access/AttendanceAccessTests.cs` | B07: check-in, check-out and breaks are for the caller's own linked employee (server times) unless the caller is an attendance admin (admin or `attendance.admin` menu); manual entries need the `AttendanceAdmin` policy; records and summaries are for self, attendance admins and the current direct manager; reports, analytics and exports are scoped on the server (self, self plus direct reports, or everyone for attendance admins), with content checks on the counts, the absenteeism head count and the CSV rows. Adds its own rows (`Access/AttendanceAccessSeed.cs`, ids 98xx); each state-changing case uses its own employee or open session. |
| `Auth/RegistrationClosedTests.cs` | B05: default settings. Self-registration returns 404 and creates nothing; invitation acceptance and sign-in still work. |
| `Auth/SignInPerIpLimitTests.cs` | B05: sign-in per-IP limit (3 in this host), the shared 429 shape, CORS headers on a 429, other endpoints unaffected. |
| `Auth/PerEmailLimitTests.cs` | B05: per-email limits for sign-in and forgot-password (2 in this host), across IPs, with email normalisation, and no counter for an email over 320 characters. |
| `Auth/OtherAuthEndpointLimitTests.cs` | B05: per-IP limits for forgot-password, refresh, reset-password and invitation acceptance (2 each in this host). |
| `Auth/RateLimitingDisabledTests.cs` | B05: `AuthRateLimiting:Enabled = false` turns every limit off. |
| `Auth/AuthRequestSizeTests.cs` | B05 review: a body over 16 KB on any anonymous auth action gets 413; normal bodies are unaffected. |

## Running

```
dotnet test EMS.API.IntegrationTests
```

## Adding a case

Most cases are one row in a fixture's `StatusCases()` (for example `Access/WhoCanDoWhatTests.cs`). The `Case(...)` helper lives on `AccessTestFixture`, so every fixture uses the same row shape.

### 1. Choose a user

Use `TestUsers`:

| User | Meaning |
|---|---|
| `TestUsers.Anonymous` | no token |
| `TestUsers.NoRole` | signed in, no roles, no linked employee |
| `TestUsers.Employee(TestSeed.E1)` | role `Employee`, linked to E1 |
| `TestUsers.Manager(TestSeed.M1)` | role `Manager`, linked to M1 |
| `TestUsers.Admin` | role `Admin`, no linked employee |
| `TestUsers.AdminWithUntrustedSigningKey` | Admin token signed with a key the API does not trust |
| `TestUsers.LinkedTo(TestSeed.E2, "HR")` | any role names, linked to a seeded employee |
| `TestUsers.Unlinked("HR")` | any role names, no linked employee |

"Linked" means the token's `sub` equals the employee's `ExternalIdentityKey`. Role names are upper-cased by EMS into role keys (`Manager` becomes `MANAGER`).

### 2. Use `TestSeed` names, never numbers

`TestSeed.O1.Id`, `TestSeed.M1.EmployeeId`, `TestSeed.E1.EmployeeId`, `TestSeed.E2.EmployeeId`.

Starting data: organisation O1; M1 (manager), E1 (reports to M1), E2 (no manager). Menus and ADMIN rights come from the real RBAC seed. `MANAGER` has the `manager.team` menu. `EMPLOYEE` has no rights.

### 3. Add the row

Allowed:

```csharp
yield return Case("B04-1", TestUsers.Manager(TestSeed.M1), HttpMethod.Get, EmployeeProfile(TestSeed.E1), HttpStatusCode.OK);
```

Denied (signed in, not allowed):

```csharp
yield return Case("B04-2", TestUsers.Employee(TestSeed.E1), HttpMethod.Get, AdminMenuAccess, HttpStatusCode.Forbidden);
```

Not signed in:

```csharp
yield return Case("B04-3", TestUsers.Anonymous, HttpMethod.Get, EmployeePortal, HttpStatusCode.Unauthorized);
```

With a JSON body (an allowed create or update). Pass the API's own request model; it is sent as camelCase JSON, the same shape the web client sends:

```csharp
yield return Case("B04-14", TestUsers.Admin, HttpMethod.Post, "/api/Departments", HttpStatusCode.Created,
    new DepartmentDTO { OrganizationId = TestSeed.O1.Id, Name = "B04 Dept", IsActive = true });
```

A denied case needs no body: authorisation runs before model binding, so the request is refused first.

`ExpectStatusAsync(user, method, path, expected, body)` takes the same optional body in a normal `[Test]`.

To check the response body as well, write a normal `[Test]` that calls `ExpectStatusAsync(...)` (it returns the body) and then `ParseJson(...)`. See the C4 to C6 tests for examples.

### 4. Check what happened behind the response

- `Factory.QueryDatabaseAsync(db => ...)` reads the EMS database in a fresh scope, for example to show a refused delete left the row in place.
- `Factory.ChangeDatabaseAsync(db => ...)` adds a fixture's own rows in a fresh scope and saves them (use it from the fixture's `[OneTimeSetUp]`).
- `Factory.RoleSyncs.Calls` lists every `SyncEmployeesForPositionAsync` and `SyncEmployeeAsync` call the API started. A "no sync ran" check is only meaningful next to an allowed case that shows a sync is recorded (see B04-01 to B04-03).

A failed case reports the user, the method and path, the expected and actual status, and the start of the response body, so the CI annotation explains the failure on its own.

## Auth endpoint and rate-limit tests

Fixtures that call the anonymous auth endpoints derive from `Harness/AuthEndpointTestFixture.cs` (itself an `AccessTestFixture`):

- **Settings per fixture.** Override `HostSettings` to give the fixture's host its own settings, for example low limits: `["AuthRateLimiting:SignInPerIp:PermitLimit"] = "3"`. They are applied after the harness defaults. Each fixture has its own host, so its limiter counters start at zero.
- **One counter per test.** Tests in a fixture share the host, so every test uses its own client IPs (the `clientIp` argument, sent as `X-Test-Client-Ip`) and its own email addresses. Use long windows (minutes) and never wait for a window to end, so a slow CI run cannot reset a counter mid-test.
- **Calling.** `PostAsync(description, path, body, clientIp, origin)` sends JSON anonymously; `PostEmptyJsonAsync` sends an empty JSON body; `GetAsAsync` calls as a `TestUser`. Each returns an `AuthCallResult` (status, body, headers).
- **Checking.** `AssertStatus`, `AssertNotRateLimited`, and `AssertRateLimited`, which checks the whole 429 contract: `Retry-After` in whole seconds (at least 1), JSON `code = rate_limited`, the fixed message, and `retryAfterSeconds` equal to the header.
- **Seeding User Management.** `SeedUserAsync(email, password)` adds a user hashed by the API's own `IPasswordHasher`; `SeedInvitationAsync(email)` adds an inactive user with a pending invitation and returns the raw token; `FindUserAsync(email)` reads one back. Underneath they use `Factory.ChangeUserManagementDatabaseAsync((db, services) => ...)` and `Factory.QueryUserManagementDatabaseAsync(db => ...)`, which mirror the EMS database helpers.

Self-registration is off by default, so never create test users through `/api/auth/register`; seed them.

## Rules

- **Cases that change data create their own rows.** All tests in a fixture share one database, and the starting cases only read. If a case creates, updates or deletes something, it adds the rows it needs first (or lives in its own fixture class deriving from `AccessTestFixture`, which gets its own database). Never change the shared seed rows.
- **Do not run fixtures in parallel.** `Program.cs` sets the static Serilog logger.
- **Files written by a fixture are removed by it.** Leave attachments live under the API's web root (or `ContentRoot/wwwroot` when the test host has none), the same place the upload and download actions use. `LeaveAccessTests` writes its seed files there and deletes its request folders in `[OneTimeTearDown]`.
- **Know what InMemory cannot do.** It has no real transactions, no filtered unique indexes, no generated `rowversion`, and no raw SQL (for example employee number allocation). Cases that depend on those are outside this harness; note them rather than forcing them through. For leave: `LeaveRequest.RowVersion` is never generated, so there are no 409 concurrency cases; a refused upload's "no file left" check relies on the API checking access before it stores the file, not on a rollback.
