# EMS.API.IntegrationTests — "who can do what"

These tests start the real EMS.API in memory and call its endpoints as different kinds of signed-in user. Each case reads "this user, calling this endpoint, gets this status". They exist so that a later change cannot quietly loosen access without CI going red.

EMS is a personal portfolio project.

## What is real and what is swapped

Real (production registrations): token issuing (`IJwtTokenService`), bearer token validation, the global "must be signed in" rule, the `AdminAccess` policy, and every EMS access service (linked employee, menu and capability checks, manager scope).

Swapped for tests only (`Harness/EmsApiFactory.cs`):

- both databases use EF Core InMemory, with a new database for each test fixture;
- background and seed hosted services are removed (the harness runs the RBAC seed itself);
- Mongo logging is turned off and email goes to a no-op sender;
- the host runs in the `Testing` environment with a test-only signing key.

## Running

```
dotnet test EMS.API.IntegrationTests
```

## Adding a case

Most cases are one row in `Access/WhoCanDoWhatTests.cs`, inside `StatusCases()`.

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

To check the response body as well, write a normal `[Test]` that calls `ExpectStatusAsync(...)` (it returns the body) and then `ParseJson(...)`. See the C4 to C6 tests for examples.

A failed case reports the user, the method and path, the expected and actual status, and the start of the response body, so the CI annotation explains the failure on its own.

## Rules

- **Cases that change data create their own rows.** All tests in a fixture share one database, and the starting cases only read. If a case creates, updates or deletes something, it adds the rows it needs first (or lives in its own fixture class deriving from `AccessTestFixture`, which gets its own database). Never change the shared seed rows.
- **Do not run fixtures in parallel.** `Program.cs` sets the static Serilog logger.
- **Know what InMemory cannot do.** It has no real transactions, no filtered unique indexes, no generated `rowversion`, and no raw SQL (for example employee number allocation). Cases that depend on those are outside this harness; note them rather than forcing them through.
