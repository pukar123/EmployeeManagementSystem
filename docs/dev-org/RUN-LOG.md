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

## 2026-11-18 — B02 Repair and verify the manager leave-approval migration
- Outcome: Done
- What changed:
  - The leave-approval database change now has its missing companion file, so EF treats it like every other migration. The database design itself is unchanged.
  - GitHub now runs a third automatic check, "Database migrations check", on every change. It starts a throwaway SQL Server, applies every migration to an empty database, and confirms the code and database model match.
  - It also confirms the leave decision fields (reviewer, review time, decision comment, clash protection) exist exactly as designed, and that the guarded legacy-table clean-up is never triggered.
  - README and CONTRIBUTING explain the new check and how to fix each kind of failure.
- Risk: High (auto-approved by CEO standing decision)
- Tests: all on GitHub: 151 of 151 .NET tests passed; no pending model changes; 33 of 33 migrations applied to an empty SQL Server 2022 database; columns verified. A deliberate model change on a throwaway branch was correctly caught. Frontend checks passed.
- Reviewer: strengths — correct and verified line for line; the check is safe (throwaway database, no secrets) and proven to catch drift. Cons — CI wouldn't spot a migration missing its companion file (Medium, new task B23); the EF tools version is set in a separate place from the packages, which matters for the .NET 10 upgrade (Medium, new task B24); minor Low items in the review.
- PR: see the B02 pull request into development
- Needs the CEO: delete the throwaway branches `probe/b01-ci-failure` and `probe/b02-pending-model` on GitHub (Branches page > bin icon); the team's session is not allowed to delete branches.

## 2026-11-19 — B03 Integration test harness for "who can do what"
- Outcome: Done
- What changed:
  - EMS now has real "who can do what" tests: each one starts the whole API in memory, signs in with a real token as a chosen kind of user (no role, employee, manager, admin, or a stranger with a fake token) and checks the answer.
  - 11 starting cases cover endpoints that were already protected: anonymous and fake-token users are refused, only admins see role permissions, employees see their own portal but not other people's profiles, and managers can see their direct reports but not others.
  - Every future security task can add its own case in one line; a short guide in the test project explains how.
  - Failure messages say exactly who called what and what came back, e.g. "Employee linked to E1 -> GET /api/RoleKeyPermissions/ADMIN: expected 200, got 403".
- Risk: Low (auto)
- Tests: on GitHub, 162 of 162 .NET tests passed (11 new); frontend and database checks green. The Tester proved a deliberately wrong case produces a clear failure message.
- Reviewer: strengths — tests use the real sign-in and permission checks, not a bypass; fast (11 tests in under a second); only one tiny production change. Cons — a real start-up bug found: outside Development EMS reads its web-access (CORS) settings from the wrong name and refuses to start, so the Production Docker image would not run (High, new task B25); tests can't send request bodies yet and write into the source folder (Medium, B26); the in-memory test database can give false confidence on concurrency and case rules (Medium, B27).
- PR: see the B03 pull request into development
- Needs the CEO: (1) consider moving B25 (start-up bug) up the order — it is small but blocks any non-Development deployment; (2) delete throwaway branches `probe/b01-ci-failure`, `probe/b02-pending-model`, `probe/b03-failing-case` on GitHub.

## 2026-11-20 — B04 Lock role assignment and admin-only setup endpoints
- Outcome: Done
- What changed:
  - Only admins can now change which roles a job position grants, so an ordinary user can no longer hand ADMIN to everyone in a position.
  - Only admins can create, change or delete organisations (including the logo), departments, sites (and who works there), locations, job positions and menus.
  - Only admins can manage leave types and leave rules, run leave accrual or the year reset, or import leave in bulk.
  - Everyone signed in can still read these lists, so dropdowns and forms keep working. No screens or database changed.
  - Four named access rules (roles, organisation setup, menus, leave admin) all use one shared "is an admin" check, so finer roles can be added later.
- Risk: High (auto-approved by CEO standing decision)
- Tests: on GitHub, 218 of 218 .NET tests passed (56 new). Every one of the 11 controllers has an "ordinary user refused" and an "admin allowed" test; the role tests also prove no roles were passed on to employees when refused. Frontend and database checks green.
- Reviewer: strengths — sound admin check that can't be forged; every write covered; strong tests. Cons — two older problems found that undercut this work: users with the ordinary `employees.access` permission can still make anyone (including themselves) an admin through two other routes (High, new task B28); the site staff list shows date of birth, phone and email to any signed-in user (High, B29); "admin" is defined three different ways across EMS (Medium, B30); the web app shows a raw "403" message and the /setup page misleads non-admins (Medium, B31); leave-admin rule mismatch added to B06.
- Release note: custom (non-admin) roles that were given setup menus can still open those screens but can no longer save.
- PR: see the B04 pull request into development
- Needs the CEO: (1) strongly consider moving B28 (self-promotion to admin) and B29 (personal-details leak) up to run next — they are small and serious; (2) delete throwaway branches `probe/b01-ci-failure`, `probe/b02-pending-model`, `probe/b03-failing-case` on GitHub.

## 2026-11-21 — B05 Close public self-registration and slow down password guessing
- Outcome: Done (2 build rounds)
- What changed:
  - Anyone could previously create an account through the API. Public sign-up is now off by default (a setting can turn it back on); staff join through invitations, which still work, and first-time setup is unaffected.
  - Sign-in, token refresh, forgot-password, reset-password, invitation acceptance and registration are now rate-limited per address, and sign-in and forgot-password also per email, so password guessing and inbox flooding are slowed down. All limits are settings with sensible defaults.
  - When a limit is hit, the app shows a friendly "Too many attempts — try again in N seconds" message, and a blocked token refresh no longer signs people out.
  - Oversized sign-in requests are refused, and the email-based limits store only a fixed-size fingerprint of each email, so they can't be used to fill the server's memory.
- Risk: High (auto-approved by CEO standing decision)
- Tests: on GitHub, 263 of 263 .NET tests passed (45 new across EMS and User Management); 48 of 48 frontend tests passed (13 new) and the frontend lint and build passed in the team workspace.
- Reviewer: strengths — limits in the right place, configurable, no personal details in errors or logs; no other anonymous route creates users; test-only parts can't reach production. Cons — one High problem introduced by this change (the email-based limiter could be used to exhaust memory) was found and fixed before merge (round 2). Remaining: behind a proxy or Docker every user may share one address, so one attacker could block sign-in for everyone, and IPv6 ranges can dodge the limits (High once proxied, new task B32); email-based limits count every attempt, so someone could keep a known user from signing in, and response times still hint whether an email exists (Medium, added to B13).
- PR: see the B05 pull request into development
- Needs the CEO: delete throwaway branches `probe/b01-ci-failure`, `probe/b02-pending-model`, `probe/b03-failing-case` on GitHub. Consider moving B28/B29 (self-promotion to admin; personal-details leak) up the order.

## 2026-11-22 — B06 Fix the Leave permission leak
- Outcome: Done
- What changed:
  - Before, anyone who could open the ordinary Leave page could view and change any employee's leave, and download their medical certificates. Now staff see and manage only their own leave.
  - A direct manager can see their team's leave requests and balances (and approve or reject, as before), but cannot create, change or cancel them, and cannot open attachments, which are usually medical certificates.
  - "Leave admin" now means one thing everywhere: an admin, or a role an admin has given the Leave Admin menu. By default only ADMIN has that menu, so nothing changes until an admin grants it to, say, an HR role, which can then run leave admin tasks.
  - The leave summary, which had no access check at all, is now leave-admin only, and uploads are checked before the file is saved.
  - The Leave Admin page no longer fires one request per employee when it is refused.
- Risk: High (auto-approved by CEO standing decision)
- Tests: on GitHub, 363 of 363 .NET tests passed (100 new, including 56 "who can do what" rows for leave). Frontend lint, 48 tests and build passed in the team workspace.
- Reviewer: strengths — every leave endpoint uses the right check, one leave-admin rule, no new route to other people's data, tests barely slower. Cons — leave admins aren't yet limited to their own organisation (Medium, new task B33); B28 must also stop people granting themselves a leave-admin role (added to B28); employees can cancel leave they've already taken and get the days back without approval (Medium, B34); a few test gaps (Low, B35). Attachment storage is still public until B08.
- PR: see the B06 pull request into development
- Needs the CEO: (1) confirm you're happy that roles you give the Leave Admin menu can run leave admin tasks (accrual, year reset, leave types) — by default only ADMIN has it; (2) delete throwaway branches `probe/b01-ci-failure`, `probe/b02-pending-model`, `probe/b03-failing-case` on GitHub.

## 2026-11-23 — B07 Make attendance safe
- Outcome: Done
- What changed:
  - Before, anyone signed in could clock in, clock out, take breaks or enter manual time for anyone. Now staff can only do these for themselves; sending someone else's employee is refused and nothing is recorded, and the server always uses the real time for ordinary staff.
  - Manual time entry and acting for someone else are for attendance admins only: admins, or a role an admin gives the new "Attendance admin" menu (given to ADMIN automatically; nobody else by default).
  - Reports, summaries and exports now show your own records, or your direct reports' too if you are a manager, or everyone for attendance admins. The limit is applied in the database, so changing the request can't widen it.
  - The attendance, reports and analytics pages hide the employee picker and manual entry for everyone except attendance admins.
- Risk: High (auto-approved by CEO standing decision)
- Tests: on GitHub, 495 of 495 .NET tests passed (132 new, including 40 "who can do what" cases for attendance). Frontend lint, 48 tests and build passed in the team workspace.
- Reviewer: strengths — every attendance action now checked on the server; same clear pattern as leave; limits applied in the database; managers lose access immediately when a report moves. Cons — attendance admins aren't limited to their own organisation (added to B33); attendance uses UTC, so a morning check-in in Sydney lands on the previous day and nobody is ever counted late (new task B36, High); two check-ins at the same moment can open two sessions (B37); managers see precise GPS locations and admin actions save the admin's location against the person (B38); no record of who set an admin time (B39); exports show raw IDs instead of names (B40).
- PR: see the B07 pull request into development
- Needs the CEO: delete throwaway branches `probe/b01-ci-failure`, `probe/b02-pending-model`, `probe/b03-failing-case` on GitHub; consider moving B28/B29 (self-promotion to admin; personal-details leak) and B36 (time zone) up the order.

## 2026-11-24 — B08 Private storage and authorised downloads for documents and leave attachments
- Outcome: Done
- What changed:
  - Employee documents and leave attachments (often medical certificates) used to sit in the public web folder, so anyone with the link could download them without signing in. They now live in a private folder outside the web folder, and the old public links return "not found". The organisation logo stays public.
  - Files can only be downloaded through signed-in download links, which keep the existing permission checks; the app no longer sends storage paths to the browser.
  - A safe, repeatable step moves existing files into private storage at start-up (on in Development and Docker; off by default in Production). It never overwrites and copes with missing files. No database change was needed.
  - One shared path check now guards every file read, write, delete and move, which also fixes a small folder-name loophole left from B06.
  - Docker keeps private files in their own volume, and your attachment-storage rule files now describe the public/private split.
- Risk: High (auto-approved by CEO standing decision)
- Tests: on GitHub, 547 of 547 .NET tests passed (52 new, including old public links returning 404 and logos still working). Frontend lint, 55 tests and build passed in the team workspace. Tests now use temporary folders instead of writing into the project.
- Reviewer: strengths — one path-safety check used everywhere; only the logo folder is public; the API refuses to start if private storage is misconfigured inside the web folder; no personal file names in logs. No new Medium or worse issues. Low items folded into existing tasks: file names in downloads (B10); Docker non-root volume, backup and a start-up warning (B14); docs refresh (B16). Documents still need per-employee access checks (B09, next).
- PR: see the B08 pull request into development
- Needs the CEO: when you next run EMS in Production, turn on `Storage:MovePublicFilesOnStartup` once so older files are moved; delete throwaway branches `probe/b01-ci-failure`, `probe/b02-pending-model`, `probe/b03-failing-case` on GitHub.

## 2026-11-25 — B09 Access checks and paging for documents
- Outcome: Done (2 build rounds)
- What changed:
  - Before, any signed-in user could list or download any employee's documents. Now employees see their own documents, managers can view their current direct reports' documents (read-only), and only document admins can upload, edit, delete or see everyone's.
  - "Document admin" means an admin, or a role an admin gives the new "Documents" menu (given to ADMIN automatically; nobody else by default).
  - Document lists are now paged (25 per page by default, up to 100).
  - New "Documents" admin page listing all documents with employee names, and a read-only "My documents" section on the employee portal (employees couldn't see their own documents before).
  - Fixed an older bug where editing a document's details failed on a real SQL Server database, and the document-expiry notification now opens the employee's own documents instead of the admin page.
- Risk: High (auto-approved by CEO standing decision)
- Tests: on GitHub, 665 of 665 .NET tests passed (118 new, including about 40 "who can do what" cases for documents). Frontend lint, 59 tests and build passed in the team workspace.
- Reviewer: strengths — every document action checked on the server; same pattern as leave and attendance; paging done in the database without extra queries. Cons — managers can open their reports' passports, visas and medical files (Medium, new task B41 "restricted document types"); document admins aren't limited to their own organisation (added to B33); the edit bug fix needs a SQL Server test (added to B27).
- Release note: roles that could see staff details (`employees.view`/`employees.manage`) no longer see documents unless an admin gives them the new Documents menu.
- PR: see the B09 pull request into development
- Needs the CEO: decide whether any HR role should get the Documents menu; delete throwaway branches `probe/b01-ci-failure`, `probe/b02-pending-model`, `probe/b03-failing-case` on GitHub.

## 2026-11-26 — B10 Safe upload rules
- Outcome: Done (2 build rounds)
- What changed:
  - Before, uploads accepted risky file types and kept whatever name and extension the uploader sent. Now one shared upload check is used for employee documents, leave attachments and the organisation logo.
  - Allowed types: documents PDF, Word (.docx), PNG and JPEG up to 15 MB; leave attachments PDF, PNG and JPEG up to 15 MB; logo PNG, JPEG and WebP up to 5 MB. SVG logos and old .doc files are no longer accepted.
  - A file is only accepted if its name, its declared type and its actual contents all agree, so a web page renamed as a PDF, a program renamed as a picture or a Word file with macros is refused with a clear message.
  - Files are saved under names the server chooses; the original name is kept only as a cleaned display name. Downloads are sent as files to save (never opened as web pages) with protective headers, including for older files.
  - Fixed a bug where uploading a new logo deleted the old one before checking the new one.
- Risk: High (auto-approved by CEO standing decision)
- Tests: on GitHub, 831 of 831 .NET tests passed (166 new, including more than 20 disguised-file cases). Frontend lint, 91 tests and build passed.
- Reviewer: strengths — one check used everywhere; type, extension and contents must all match; downloads can never run as a web page; no personal file names in logs. One Medium problem introduced (a specially built Word file could use a lot of memory before being refused) was fixed before merge in round 2. Low items folded into B14 (proxy upload size) and B15 (extra protective header).
- Release note: an existing SVG logo will no longer show; re-upload it as PNG, JPEG or WebP.
- PR: see the B10 pull request into development
- Needs the CEO: re-upload the organisation logo if it is an SVG; delete throwaway branches `probe/b01-ci-failure`, `probe/b02-pending-model`, `probe/b03-failing-case` on GitHub.

