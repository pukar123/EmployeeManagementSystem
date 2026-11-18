# Contributing and Git workflow

## Branch model: Development → Pre-Prod → Prod

| Branch | Role |
|--------|------|
| `development` | Integration branch. All feature work merges here via pull request. |
| `pre-prod` | UAT / stabilization. Promoted from `development` after QA. |
| `prod` | Production-aligned code. Promoted from `pre-prod` after UAT sign-off. |

Do **not** commit directly to `development`, `pre-prod`, or `prod`. Use **feature branches** cut from `development`, then delete the feature branch after the PR is merged.

```text
feature/*  →  (PR)  →  development  →  (promote)  →  pre-prod  →  (promote)  →  prod
```

### Daily developer flow

1. `git fetch origin` and `git checkout development` then `git pull`.
2. `git checkout -b feature/short-description`.
3. Commit and `git push -u origin feature/short-description`.
4. Open a **Pull Request** into **`development`**. After review, merge the PR.
5. Delete the remote feature branch (GitHub offers this after merge). Locally: `git checkout development && git pull && git branch -d feature/short-description`.

### Release promotions

- **development → pre-prod**: Open a PR when the dev build is ready for UAT; merge after QA.
- **pre-prod → prod**: Open a PR after UAT approval; tag releases on `prod` as needed (e.g. `v1.0.0`).

### Long-lived branches on GitHub

These branches already exist on `origin`: `development`, `pre-prod`, `prod` (initially aligned with `master`). New work targets **`development`** by default.

---

## GitHub one-time setup (default branch + protection)

Enforcement (“never touch” long-lived branches) is done on GitHub with **branch protection** and by making **`development`** the **default branch**.

### Option A — Script (recommended)

1. Create a [Personal Access Token](https://github.com/settings/tokens) (classic) with scope **`repo`** (full control of private repositories), or a fine-grained token with **Administration** and **Contents** on this repository.
2. In PowerShell from the repo root:

   ```powershell
   $env:GITHUB_TOKEN = "your_token_here"
   .\scripts\Set-GitHubBranchPolicies.ps1
   ```

   This sets the default branch to **`development`** and applies protection to **`development`**, **`pre-prod`**, and **`prod`**: pull request required before merge, force-push disabled, branch deletion disabled.

### Option B — Web UI

1. **Default branch**  
   Repository **Settings** → **General** → **Default branch** → switch to **`development`** → **Update**.

2. **Branch protection** (repeat for each branch: `development`, `pre-prod`, `prod`)  
   **Settings** → **Branches** → **Add branch protection rule**  
   - **Branch name pattern**: exact branch name (e.g. `development`).  
   - Enable **Require a pull request before merging** (set required approvals as your team needs).  
   - Enable **Do not allow bypassing the above settings** (optional but strict).  
   - Disable **Allow force pushes**.  
   - Enable **Do not allow deletions** (if available in your plan/UI).

   Alternatively use **Rules** → **Rulesets** (new UI) with the same intent for those three branches.

---

## Optional tooling

- Install [GitHub CLI](https://cli.github.com/) (`gh`) for PRs and API tasks from the terminal.

## Continuous integration

GitHub Actions (`.github/workflows/ci.yml`) runs on every push and every pull request:

- **Backend build and test**: `dotnet restore`, `dotnet build` and `dotnet test` for `EmployeeManagementSystem.sln` (.NET 9). Compile errors and failed tests show as annotations on the check run; the test result (`.trx`) files are kept as a workflow artifact for 7 days.
- **Frontend lint, test and build**: `npm ci`, `npm run lint`, `npm test` and `npm run build` in `ems-web` (Node 20). Lint errors, failed tests and build errors show as annotations.

- **Database migrations check**: builds `EMS.API` (Release), then with `dotnet-ef` 9.0.4 runs `dotnet ef migrations has-pending-model-changes` (the EF model must match the latest migration snapshot) and applies every EMS migration to a new, empty SQL Server 2022 database in a throwaway service container. It then checks the `org.LeaveRequests` decision columns, that `__EFMigrationsHistory` has one row per migration ending with the newest one, and that the `[emp].[_UmLegacyDropApproved]` marker does **not** exist. CI never creates that marker.

All three checks must be green before a pull request is merged into `development`.
