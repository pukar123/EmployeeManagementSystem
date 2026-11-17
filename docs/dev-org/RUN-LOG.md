# EMS dev-org run log

## 2026-11-16 — (no task) scheduled run blocked
- Outcome: Blocked
- What changed: nothing. The cloud scheduled run could read the repo but could not push (its fresh session lacked repository push access). A blocker email was sent.
- Needs the CEO: pause the old "EMS daily build" scheduled task; nightly builds now run from the delivery manager's conversation, which has push access.

## 2026-11-17 — B01 Automatic build and test check, and tidy leftovers
- Outcome: Done
- What changed:
  - Every push and pull request now gets an automatic GitHub check that compiles the whole .NET solution, runs all .NET tests, and runs the frontend lint, tests and production build.
  - Failures show up on GitHub by name: failing tests, compile errors (file and line) and lint errors.
  - The Departments update now handles a duplicate-name clash in the business layer instead of the controller, with the same response as before and three new tests.
  - Template leftovers (WeatherForecast, Class1, UnitTest1, the fake integration test) and 244 build output files committed by mistake are gone; build output folders are now ignored.
  - Before this, the roadmap branch (manager leave approval plus the dev-org plan) was merged into development (PR #1).
- Risk: Medium (auto-approved by CEO standing decision)
- Tests: for the first time EMS was compiled and tested for real, on GitHub: 151 of 151 .NET tests passed in 5 test projects. Frontend checks also ran in the team workspace: lint 0 errors, 35 of 35 tests passed, build OK. The Tester deliberately broke a test, the compile and lint on a throwaway branch and confirmed each failure was reported. The manager leave-approval feature compiled cleanly.
- Reviewer: strengths — safe, read-only CI with no secrets; clear failure reports; Departments fix in the right layer with tests. Cons — .NET 9 is out of Microsoft support since 10 Nov 2026 (Medium, new task B21); Node 20 is past end of life (Medium, new task B22); smaller Low items noted in the review (CI runs twice per PR commit; actions pinned by tag; nothing enforces "green before merge" yet).
- PR: see the B01 pull request into development
- Needs the CEO: (1) delete the throwaway branch `probe/b01-ci-failure` on GitHub (Branches page > bin icon) — it holds deliberately failing commits and must not be merged; the team's session was not allowed to delete branches. (2) Optional: turn on branch protection for `development` requiring the "Backend build and test" and "Frontend lint, test and build" checks, so nothing can merge while red.
