# Employee Management System — End User Guide

This guide explains how to use the **Employee Management System (EMS)** web application. It covers every feature available today and walks through common day-to-day tasks.

**Audience:** HR staff, operations managers, system administrators, and employees using the self-service portal.

---

## Table of contents

1. [What EMS does](#1-what-ems-does)
2. [Signing in](#2-signing-in)
3. [First-time organization setup](#3-first-time-organization-setup)
4. [Navigation and access](#4-navigation-and-access)
5. [Home dashboard](#5-home-dashboard)
6. [Organization profile](#6-organization-profile)
7. [Employees](#7-employees)
8. [Employee transfers](#8-employee-transfers)
9. [Departments](#9-departments)
10. [Job positions](#10-job-positions)
11. [Sites](#11-sites)
12. [Attendance](#12-attendance)
13. [Leave](#13-leave)
14. [Tasks](#14-tasks)
15. [User management](#15-user-management)
    - Employee capabilities editor at `/user-management/employee-capabilities`
16. [Employee portal (self-service)](#16-employee-portal-self-service)
17. [Recommended setup order](#17-recommended-setup-order)
18. [Common workflows](#18-common-workflows)
19. [Known limitations](#19-known-limitations)

---

## 1. What EMS does

EMS is a single place to manage your workforce and day-to-day operations:

- **People:** employee records, department and position transfers, employment status
- **Structure:** departments, job positions, work sites, organization branding
- **Operations:** attendance (check-in/out, breaks, reports), leave requests, task assignment
- **Access control:** users, roles, and menu-based permissions
- **Self-service:** a simplified **Employee Portal** for linked employees

The application is designed for **one organization per installation**. All data belongs to that organization.

---

## 2. Signing in

1. Open the EMS web address provided by your administrator (for local development this is typically `http://localhost:3000`).
2. Go to **Login** (`/login`).
3. Enter your **email** and **password**.
4. After signing in, you are taken to the **Home** page or to **organization setup** if no organization exists yet.

### Change your password

- Use **Change password** (`/change-password`) from the user menu in the header.
- If an administrator provisioned your account, you may receive a **temporary password** the first time you sign in. Change it before continuing.

### Sign out

- Open the **user menu** (top-right) and choose **Sign out**.

---

## 3. First-time organization setup

Before any other module works, an administrator must create the organization.

1. After login, if no organization exists, you are directed to **Create organization** (`/setup`).
2. Fill in:
   - **Name** and **Code** (required identifiers)
   - **Description** and **Motto** (optional)
   - **Active** status
   - **Logo** (optional image upload)
3. Save. You can update these details later under **Organization** (`/organization/setup`).

Until setup is complete, only **Home** and **Create organization** appear in the sidebar.

---

## 4. Navigation and access

### Sidebar menus

After organization setup, the left sidebar shows the modules your role is allowed to use. Typical menus include:

| Menu | Route | Purpose |
|------|-------|---------|
| Home | `/` | Dashboard and quick links |
| Employees | `/employees` | Employee records |
| Departments | `/departments` | Department hierarchy |
| Attendance | `/attendance` | Check-in/out and history |
| ↳ Reports | `/attendance/reports` | Attendance summaries and export |
| ↳ Analytics | `/attendance/analytics` | Punctuality and absenteeism |
| Leave | `/leave` | Submit and view leave requests |
| ↳ Leave Admin | `/leave/admin` | HR overview, manual entry, import |
| ↳ Leave Setting | `/leave/admin/settings` | Configure leave types |
| Tasks | `/tasks` | Assign and track work |
| Task Calendar | `/tasks/calendar` | Calendar view of tasks |
| Positions | `/positions` | Job titles and role inheritance |
| Sites | `/sites` | Physical work locations |
| Organization | `/organization/setup` | Company profile and logo |
| User management | `/user-management` | Users, roles, menu access |

**Employee Transfers** (`/employee-transfers`) is reached from the Employees area; it is not a separate top-level menu item.

### Who sees what

Access is controlled by **roles** and **menu permissions**:

- The built-in **ADMIN** role can see every menu.
- Other roles only see menus granted in **User management → Menu access**.
- Employees with a linked login account can also use the **Employee Portal** (`/employee-portal`).

If you cannot see a module you expect, ask your administrator to check your **role assignments** and **menu access**.

### Theme

Use the **theme toggle** in the header to switch between light and dark mode.

---

## 5. Home dashboard

**Route:** `/`

The home page gives a quick overview:

- **Stat cards** — total employees and departments (click a card to open that module).
- **Quick links** — shortcuts to Positions, Tasks, Attendance, and Leave.
- **Organization link** — create or update organization details if needed.
- **Employee portal** — if your account is linked to an employee record, a link to open the self-service portal.

---

## 6. Organization profile

**Routes:** `/setup` (first create) · `/organization/setup` (edit)

### Create or update

1. Open **Organization** from the sidebar (or **Create organization** from Home).
2. Edit name, code, description, motto, and active flag.
3. **Upload or replace the logo** — the logo appears in the sidebar and header when configured.
4. Save changes.

---

## 7. Employees

**Routes:** `/employees` (directory), `/employees/{id}` (profile)

Manage your workforce as lifecycle records—not just table rows.

### Directory (`/employees`)

- **Server-backed search** across name, email, employee number, and phone (mixed case and surrounding spaces are trimmed).
- **Filters:** employment status (including Preboarding), department, position, manager, site, and login-link status.
- See the **result count** and **active filter chips**; use **Clear all filters** to reset.
- **Archive view** shows archived employees with **Restore** when retention rules allow.
- **Export CSV** downloads the current filtered set (not only the current page); requires the **Export employee directory** capability.
- **Sortable columns** and **page size** controls are available in the directory header and pagination row.
- Click a name or **View profile** to open `/employees/{id}`.
- Employee numbers (for example `EMP014`) are stable identifiers in the directory—not row indexes.

### Employee capabilities (administrators)

The **Employees** menu controls navigation only. Data access is granted separately under **User management → Employee capabilities**:

| Capability | Allows |
|------------|--------|
| **View employees** | Directory, profiles, history |
| **Manage employees** | Create, edit, lifecycle, transfers, scheduled changes |
| **Employee account access** | Invitations, link user, roles, reactivate login |
| **Export employee directory** | CSV export |

Manage, account access, and export automatically include view.

### Login invitations

From the profile **Access** tab, users with account access can **Send invitation**. The employee receives an email with a single-use link (`/accept-invitation`) that expires in 24 hours. Delivery failures do not show success—the invitation stays pending for retry. **Restore** reopens the employee record only; login remains disabled until **Reactivate login** (active employees with account access).

### Scheduled changes

Future-dated terminations, archives, status changes, and transfers are scheduled from the profile (or via API) and appear under **Upcoming changes**. They apply automatically on the effective date (business timezone, default Australia/Sydney). Pending scheduled changes can be cancelled before they run.

### Employee profile (`/employees/{id}`)

Open any employee directly by URL. The profile has tabs:

| Tab | Contents |
|-----|----------|
| **Overview** | Contact details and key employment summary; **Edit profile** for personal/contact fields only |
| **Employment** | Organization assignment, dates, linked **sites**; **Transfer** for department, position, or manager changes |
| **History** | Readable timeline (names, effective dates, reasons, who made the change) |
| **Access** | Linked sign-in account status, inherited and direct roles; send login invitation or link an account |
| **Documents** | Placeholder for a future documents experience |

**Lifecycle actions** (on the profile, not in ordinary edit):

| Status | Meaning |
|--------|---------|
| Preboarding | Hired, not yet started |
| Active | Currently employed |
| Inactive | Temporarily not working |
| Terminated | Employment ended (requires effective date and reason) |
| Archived | Hidden from active lists; separate from termination |

Use **Change status**, **Terminate**, **Archive**, and **Restore**—never **Delete**. Archiving does not automatically terminate employment.

### Add an employee (wizard)

1. Click **Add employee** on the directory.
2. Complete sections: **Personal** → **Employment** → **Organization** → **Review**.
3. Required fields are marked; optional fields include address **location** (geographical record) and work **sites** (assigned separately).
4. If a possible duplicate is found (same email, phone, or name + date of birth), review warnings such as “This email is already used by employee EMP014” before confirming.
5. After creation, choose **View profile**, **Send login invitation**, or **Add another employee**. Temporary passwords are not shown in the UI.

### Edit profile

- Personal and contact fields only on the profile **Edit** action.
- **Employment status** and **department / position / manager** are changed through dedicated lifecycle and **Transfer** actions—not the profile form.

### Terminology

| Term | Meaning |
|------|---------|
| **Sign in / Sign out** | EMS account session |
| **Site** | Work site (many-to-many on the employee) |
| **Address location** | Geographical address record (`location` in forms) |
| **Archive** | Soft-hide a retained record (not delete) |

### Link a sign-in account

From the profile **Access** tab:

1. **Send login invitation** provisions a new account (no predictable password shown).
2. If an account already exists, use **Link existing account** with the user ID from User management.
3. Confirm **role** changes; inherited roles come from the job position.

---

## 8. Employee transfers

Transfers are recorded from the employee profile **Employment** tab (the former `/employee-transfers` route redirects to the directory).

1. Open the employee profile → **Employment** → **Transfer**.
2. Choose **department**, **position**, or **manager**.
3. Enter effective date and reason; confirm current vs new values.
4. The change appears immediately in **History** with readable names.

You need existing departments and positions before transferring.

---

## 9. Departments

**Route:** `/departments`

Maintain your org chart structure.

### Create a department

1. Click **Add department**.
2. Enter **name**, **code**, optional **parent department** (for hierarchy), and **active** flag.
3. Save.

### Edit, delete, and search

- Update any department from the list.
- Delete departments you no longer need (subject to validation if employees are assigned).
- Use **search** to find departments quickly.

---

## 10. Job positions

**Route:** `/positions`

Define job titles used when creating employees.

### Create a position

1. Click **Add position**.
2. Enter **title**, **code**, **description**, and **active** flag.
3. Save.

### Assign roles to a position

Positions can carry **default roles**. Employees assigned to that position **inherit** those roles automatically.

1. Open a position.
2. Assign one or more **roles**.
3. Save. New and existing employees on that position receive inherited access (unless overridden per employee).

---

## 11. Sites

**Route:** `/sites`

Manage physical or logical work locations.

1. **Add a site** with name, location, description, and active flag.
2. **Edit** or **delete** sites from the list.
3. **Search** to filter sites.

Sites can be linked to employees when editing their profile.

---

## 12. Attendance

**Routes:** `/attendance` · `/attendance/reports` · `/attendance/analytics`

### Daily attendance (`/attendance`)

1. **Select an employee** (HR/admin can work on behalf of any employee).
2. Choose a **date range** for the history table.
3. Use the action cards:
   - **Check in** — starts a work session. GPS coordinates are captured if the browser allows location access.
   - **Check out** — ends the current session.
   - **Start break** / **End break** — track break time within an open session.
   - **Manual entry** — add or correct a record for a specific date and time (administrative use).
4. Review the **summary** (worked minutes, break minutes) and **history table** for the selected period.

If location permission is denied, check-in and check-out still work; coordinates are simply omitted.

### Reports (`/attendance/reports`)

- Filter by **employee**, **department**, **date range**, and grouping (daily / weekly / monthly).
- View **summary cards** and a **trend chart**.
- **Export** the report in the available format for sharing or archiving.

### Analytics (`/attendance/analytics`)

- Review **punctuality** metrics (late arrivals, early departures).
- Review **absenteeism** trends.
- Adjust filters to focus on a team or time period.

---

## 13. Leave

**Routes:** `/leave` · `/leave/admin` · `/leave/admin/settings` · `/employee-portal/leave`

Leave is split between everyday requests, HR administration, and configuration.

> **Note:** Multi-step manager **approve/reject workflow** is not the primary path today. Requests are created, tracked, and can be cancelled; HR can also enter leave manually or via bulk import.

### Configure leave types first (`/leave/admin/settings`)

Before employees request leave:

1. Open **Leave → Leave Setting**.
2. **Add a leave type** — name, unit (**Days** or **Hours**), whether an attachment is required, and active flag.
3. Edit or retire types as policies change.

### Submit a leave request (`/leave`)

1. Open **Leave**.
2. If you manage others’ leave, **select the employee**; otherwise the form applies to your linked employee record.
3. Choose **leave type**, **start date**, **end date**, **amount** (days or hours), and **reason**.
4. Check **available balance** for the selected type before submitting.
5. Submit the request. It appears in your request list with status **Pending** (or similar).
6. **Cancel** a request from the list if plans change.

### Leave admin (`/leave/admin`)

For HR and administrators:

- **Summary cards** — applied (pending), approved, and who is on leave today.
- **Manual leave entry** — record leave on behalf of any employee.
- **Bulk import** — paste CSV-style lines:
  ```
  employeeId,leaveTypeId,startDate,endDate,amount
  ```
  One row per request. Use employee and leave type IDs from the system.

### Employee portal leave (`/employee-portal/leave`)

Linked employees can view balances, recent requests, and submit or cancel their own leave without the full admin sidebar.

---

## 14. Tasks

**Routes:** `/tasks` · `/tasks/calendar`

Assign and track work across the team.

### Create a task

1. Open **Tasks**.
2. Click **Add task** (or equivalent).
3. Set **employee**, **title**, **description**, **start date**, **due date**, and **priority**.
4. Save.

### Manage tasks

- **Table view** (`/tasks`) — sortable list with filters for search text and employee.
- **Calendar view** (`/tasks/calendar`) — see tasks on a calendar.
- **Update status:** Assigned → **In progress** → **Complete**, or mark **Blocked**.
- **Delete** tasks that are no longer needed.

Employees see and update their own tasks in the **Employee Portal** as well.

---

## 15. User management

**Routes:** `/user-management/users` · `/user-management/roles` · `/user-management/menu-access`

Administrator-only area for accounts and access control.

### Users

1. Open **User management → Users**.
2. **Create** a user with email, display name, and active flag.
3. **Edit** existing users.
4. **Assign roles** to control what they can access.
5. **Set or reset password** — passwords are never shown after creation; use reset for forgotten credentials.

### Roles

1. Open **Roles**.
2. **Create**, **edit**, or **delete** custom roles (name and description).
3. **System roles** (such as ADMIN) are protected and cannot be edited or deleted.

### Menu access

1. Open **Menu access**.
2. Select a **role**.
3. Use the **tree** of menus — expand branches, check or uncheck items, or use **Select all** / **Clear all** on a branch.
4. Parent menus are included automatically when a child is selected.
5. **Save**. Users with that role will only see granted items in the sidebar.

---

## 16. Employee portal (self-service)

**Routes:** `/employee-portal` · `/employee-portal/leave`

A simplified experience for employees whose login is **linked** to an employee record.

### Portal home

- **Next work** — highlights the upcoming shift or task.
- **Schedule** — interleaved list of shifts and open tasks.
- **Start** — begin the next eligible shift or task (when status allows).
- **Leave summary** — balances and recent requests with a link to **My leave**.

### My leave

Full self-service leave form scoped to the signed-in employee (no employee picker).

### Return to admin app

Use **Main app** in the portal header to go back to `/` (if your role has admin menus).

### Not linked?

If you see a message that your account is not linked to an employee, ask HR to use **Link login** on your employee record in the main **Employees** module.

---

## 17. Recommended setup order

For a new EMS installation, follow this order:

1. **Sign in** as administrator.
2. **Create the organization** (`/setup`).
3. **User management → Roles** — create roles for HR, managers, employees (if not using only ADMIN).
4. **User management → Menu access** — grant each role the menus they need.
5. **Departments**, **Positions**, and **Sites** — build structure before adding people.
6. **Positions → assign roles** — so job titles carry the right access.
7. **Leave → Leave Setting** — add at least one leave type.
8. **Employees** — add staff and **link login** accounts where self-service is needed.
9. **Attendance**, **Tasks**, and **Leave** — begin daily operations.

---

## 18. Common workflows

### Onboard a new hire

1. Create **department** and **position** if they do not exist.
2. **Add employee** with join date and employment status **Active**.
3. **Link login** and note the temporary password.
4. Assign **roles** (or rely on position-inherited roles).
5. Tell the employee to sign in, **change password**, and open the **Employee Portal**.

### Move someone to a new team

1. Go to **Employee transfers**.
2. Select the employee.
3. Submit a **department transfer** (and **position transfer** if their title changes) with effective date and reason.
4. Confirm the update in **employee history**.

### Record attendance for someone who forgot to check in

1. Open **Attendance**.
2. Select the employee and date range.
3. Open **Manual entry** and enter check-in/check-out times.
4. Save and verify the history table.

### Grant a manager access to leave and attendance only

1. Create a **Manager** role under **Roles**.
2. Under **Menu access**, grant Home, Attendance (and sub-menus if needed), and Leave.
3. Assign the role to the manager’s user (directly or via their job position).

### Import historical leave

1. Open **Leave Admin**.
2. Prepare CSV lines: `employeeId,leaveTypeId,startDate,endDate,amount`.
3. Paste into the import area and run the import.
4. Review summary counts and employee leave lists.

---

## 19. Known limitations

These items are **not** available in the current web UI or are intentionally deferred:

| Area | Current behavior |
|------|------------------|
| Leave approval workflow | No dedicated approve/reject UI; requests stay in pending-style states until changed by admin processes or import. |
| Shift scheduling | Shifts can appear in the Employee Portal when created via the API, but there is **no admin screen** to schedule shifts in the web app yet. |
| Recruiting / hiring pipeline | No job requisitions, candidates, or offer management. |
| Employee documents | Document upload API exists; no documents screen in the web app yet. |
| Multi-organization | One organization per installation. |

For technical setup (API URL, Docker, development login), see the [repository README](../README.md) and [ems-web frontend guide](../ems-web/docs/FRONTEND.md).

---

## Document information

| | |
|---|---|
| **Purpose** | End-user working guide for EMS web features |
| **Update when** | New menus, workflows, or portal capabilities ship |
| **Related docs** | [Business perspective](business-perspective.md) · [Architecture](architecture.md) |

*Last updated: June 2026*
