# EMS.API.IntegrationTests — "who can do what"

These tests start the real EMS.API in memory and call its endpoints as different kinds of signed-in user. Each case reads "this user, calling this endpoint, gets this status". They exist so that a later change cannot quietly loosen access without CI going red.

EMS is a personal portfolio project.

## What is real and what is swapped

Real (production registrations): token issuing (`IJwtTokenService`), bearer token validation, the global "must be signed in" rule, the admin policies (`AdminAccess`, `RoleAdmin`, `OrganizationAdmin`, `MenuAdmin`, `LeaveAdmin`), and every EMS access service (linked employee, menu and capability checks, manager scope).

Swapped for tests only (`Harness/EmsApiFactory.cs`):

- both databases use EF Core InMemory, with a new database for each test fixture;
- background and seed hosted services are removed (the harness runs the RBAC seed itself);
- Mongo logging is turned off and email goes to a no-op sender;
- the host runs in the `Testing` environment with a test-only signing key;
- the real role sync service (`IEmployeeRoleSyncService`) is wrapped, not replaced, by `RecordingEmployeeRoleSyncService`, which notes every call in `Factory.RoleSyncs` and then runs the real service.

## Fixtures

| Fixture | What it covers |
|---|---|
| `Access/WhoCanDoWhatTests.cs` | Read-only "who can do what" cases on the shared `TestSeed` (B03). |
| `Access/AdminSetupAccessTests.cs` | B04: role assignment and setup writes are admin-only, their reads stay open. Adds its own rows (`Access/AdminSetupSeed.cs`), so its writes never touch the shared seed. |

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

## Rules

- **Cases that change data create their own rows.** All tests in a fixture share one database, and the starting cases only read. If a case creates, updates or deletes something, it adds the rows it needs first (or lives in its own fixture class deriving from `AccessTestFixture`, which gets its own database). Never change the shared seed rows.
- **Do not run fixtures in parallel.** `Program.cs` sets the static Serilog logger.
- **Know what InMemory cannot do.** It has no real transactions, no filtered unique indexes, no generated `rowversion`, and no raw SQL (for example employee number allocation). Cases that depend on those are outside this harness; note them rather than forcing them through.
