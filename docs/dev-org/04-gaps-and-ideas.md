# 04 — Gap Analysis and Distinctive Ideas

**Prepared by:** Gap & Innovation Strategist (Business Analysis function)
**Date:** 15 November 2026
**Inputs:** `01-market-research.md` (market), `02-system-inventory.md` (EMS today), `03-project-review.md` (strengths and cons). Cross-references use those documents' numbering: matrix row numbers (#1–#47), inventory sections (§1.x) and review cons (C1–C23).
**Scope note:** EMS is a personal portfolio project. "Target user" below means the product EMS is designed *as if* it served: Australian businesses of 5–50 staff that want compliance first and explainable results. This document does not score feasibility or set priorities. The Product Owner does that separately.

---

## 1. Gap analysis

**Status key:** **Have** = works end to end today. **Partial** = some of it exists, or it exists with gaps or defects. **Missing** = not present.
**Class key (for the 5–50 staff, compliance-first, explainable target):**
- **Table stakes (TS):** buyers expect it. Missing it rules EMS out.
- **Differentiator (D):** thinly served by the market, or a place where EMS can be clearly better.
- **Skip:** out of scope for the target, or better handled through integration.

### 1.1 Market capabilities vs EMS

| # | Capability | EMS status | Evidence / note | Class |
|---|---|---|---|---|
| 1 | Employee records / core HR | Partial | Strong lifecycle and effective-dated history (§1.1). No employment type, standard hours, address, emergency contact, award/classification or super fields | TS |
| 2 | Custom fields | Missing | Fixed schema | Skip (for now) |
| 3 | Org chart | Partial | Manager and department hierarchies exist in data. No chart view | TS |
| 4 | Employee self-service (web) | Partial | Portal has schedule, leave balances and requests. No own profile, own documents or self check-in (§1.10) | TS |
| 5 | Mobile app | Missing | Responsive only. No PWA, offline mode or push (§4 of inventory) | TS (PWA is enough) |
| 6 | Onboarding checklists | Have | Templates, generated tasks, progress widget (§1.3) | TS |
| 7 | FWIS / CEIS delivery in onboarding | Missing | Could be a checklist item today, but with no evidence of delivery | D |
| 8 | Offboarding | Missing | Terminate, archive and scheduled changes exist, but no checklist or access-removal steps | TS |
| 9 | Documents / policy library | Partial | Employee documents with expiry reminders. Files are publicly served (C3). No organisation policy library | TS |
| 10 | E-signature / acknowledgement | Missing | — | TS (acknowledgement is enough) |
| 11 | Leave requests and approvals | Have | Manager approval, self-approval block, concurrency, notifications (§1.7) | TS |
| 12 | NES-correct leave types and accrual | Partial | Generic types and accrual engine. No NES seeding, no ordinary-hours basis, no casual exclusion. Accrual and year-reset defects (C4, §1.7) | TS |
| 13 | Public holidays by state | Missing | Leave amounts are typed in by the user | TS |
| 14 | Clock in/out (GPS, kiosk) | Partial | GPS check-in and breaks. No authorisation (C1), admin-style picker, no self check-in, no kiosk | TS |
| 15 | Timesheets and approval | Missing | `AutoApproved` status exists with no flow | TS |
| 16 | Break / fatigue rules | Partial | Breaks recorded, policy minutes exist. No rule checks or warnings | D |
| 17 | Rostering | Partial | Shift CRUD, overlap prevention, calendar. No templates, recurrence or publish step (§1.6) | TS (basic) |
| 18 | Shift swap / open shifts / availability | Missing | — | D |
| 19 | Demand forecasting | Missing | — | Skip |
| 20 | Award interpretation / penalty rates | Missing | Market advice: don't build an engine | Skip (explain and integrate instead; see Idea 15) |
| 21 | Native payroll | Missing | — | Skip |
| 22 | Single Touch Payroll | Missing | — | Skip (payroll system lodges) |
| 23 | Super / Payday Super | Missing | No fund fields | TS (capture and pass to payroll only) |
| 24 | Payroll / accounting integration | Missing | Outbox pattern exists and could carry it (§1.16) | TS |
| 25 | Performance reviews | Missing | — | Skip |
| 26 | Goals / OKRs | Missing | — | Skip |
| 27 | Learning / LMS | Missing | — | Skip |
| 28 | Qualifications / licence expiry | Partial | Document expiry dates and reminder worker. Not linked to rostering | D |
| 29 | Recognition / kudos | Missing | — | Skip |
| 30 | Surveys / eNPS | Missing | — | Skip |
| 31 | Configurable workflow engine | Partial | Hard-coded leave approval, scheduled changes, tasks | Skip (fixed, well-explained flows suit the target) |
| 32 | Reporting and dashboards | Partial | Rich attendance analytics and exports. No leave liability, headcount or turnover reports. Home dashboard has counts only (§1.15) | TS |
| 33 | Audit trail / change history | Partial | Every EF change is captured, but there is no way to read it and sensitive fields are not filtered (§1.14) | TS (D if made readable and explainable) |
| 34 | AI assistant (Q&A) | Missing | — | TS (by 2026) |
| 35 | AI agent actions, human-approved | Missing | — | D |
| 36 | Open API / webhooks | Partial | Full REST API. OpenAPI only in Development, no webhooks | D (for open/self-host positioning) |
| 37 | SSO / SCIM | Missing | — | Skip (Microsoft/Google sign-in may matter later) |
| 38 | Role-based and field-level permissions | Partial | Good model, uneven enforcement (C1, C2, C5) | TS |
| 39 | WHS incident reporting | Missing | — | D |
| 40 | Right-to-disconnect controls | Missing | Notification library exists to build on | D |
| 41 | 7-year retention and inspector export | Partial | `RetentionUntilUtc` set on archive. No purge or anonymise job and no export pack | D |
| 42 | Assets / equipment | Missing | — | Skip |
| 43 | Expenses | Missing | — | Skip |
| 44 | Recruitment / ATS | Missing | Out of scope by design | Skip |
| 45 | Compensation / benefits | Missing | — | Skip |
| 46 | Directory / announcements | Partial | Paged directory, duplicate detection, command palette. No announcements | TS (directory); announcements optional |
| 47 | Multi-location | Partial | Sites and locations exist. `Site` is not tied to an organisation, no per-site time zone, single tenant only | TS (multi-site within one business) |

### 1.2 Capabilities outside the matrix worth noting

| Capability | EMS status | Note | Class |
|---|---|---|---|
| Email / push notifications | Missing | In-app only, polled every 60 seconds (§1.13) | TS |
| Future-dated (scheduled) employee changes | Have | Uncommon in small-business tools; already a quiet strength | D (existing) |
| Effective-dated history of department, position and manager | Have | Supports "what was true on date X" questions that inspectors ask | D (existing) |
| Integration outbox with idempotency | Have | A sound base for payroll export and webhooks | Enabler |
| Agreements register (TOIL, cash-out, leave in advance, IFAs, flexible work requests) | Missing | Part of Fair Work record-keeping duties (01 §6) | D |
| Data export and exit (no lock-in) | Partial | Employee CSV and attendance exports only | D |

### 1.3 Reading the gap

- **Table-stakes gaps** fall into five groups: (a) AU leave correctness (NES types, public holidays, working-day calculation, the accrual defects); (b) time (self check-in, timesheet approval); (c) the employee record (employment type, hours, super fund); (d) payroll export; (e) a readable audit trail, real reports and email/push notifications. An AI Q&A assistant is now also expected.
- **Differentiator gaps line up with the "nobody owns this" rows** in the market matrix: #7, #16, #28, #39, #40 and #41. EMS already has building blocks for each: onboarding checklists, attendance breaks, document expiry, the notification library, the audit interceptor and retention dates.
- **Existing quiet strengths** (scheduled changes, effective-dated history, the outbox, the audit interceptor) are exactly what explainable compliance needs. Most of the ideas below put these to work rather than adding new subsystems.

---

## 2. Unique ideas

Each idea says what problem it solves, who benefits, why competitors don't do it well, which existing EMS parts it builds on, and the rough shape of the solution. They are not ranked. Ideas 1–7 are about compliance as a feature, 8–12 about explainability and bounded AI, 13–16 about frontline, regional and multilingual workforces, and 17–19 about positioning.

### Idea 1 — Fair Work Inspector-Ready Records Pack
- **Problem:** When a Fair Work inspector asks for records, a small business has 14 days or less to produce them. They end up scrambling through spreadsheets, payroll and email. If records are missing, courts presume the employee's claim is correct, and intentional underpayment has been a crime since January 2025.
- **Who benefits:** Owners and office managers, and their accountant or adviser.
- **Why competitors fall short:** AU suites keep the records but treat export as a set of generic reports (matrix #41). Global and open-source tools have no idea what an inspector asks for.
- **Builds on:** The audit interceptor (§1.14), effective-dated employee history, attendance exports (CSV/XLSX/PDF), leave balances and requests, `RetentionUntilUtc`.
- **Shape:** Choose an employee (or all employees) and a date range, then get one bundle. It holds a cover index mapped to each record-keeping item (employment basis, hours, leave, agreements, termination). It includes each record "as at" the requested dates, a correction log showing every change with who, when and why, and a list of any gaps ("no hours recorded for 3 March"). Producing the pack is itself recorded in the audit trail. Payroll-held items (pay rates, super paid) are listed as "held in Xero Payroll" with a link rather than left out silently.

### Idea 2 — FWIS / CEIS Proof of Delivery
- **Problem:** Businesses must give every new starter the Fair Work Information Statement, and casuals the Casual Employment Information Statement at the start and at set points after. Most keep no evidence that they did.
- **Who benefits:** Employers (evidence in a dispute) and employees (they actually receive their rights information).
- **Why competitors fall short:** Only a few AU vendors handle this (matrix #7), usually as a document attachment with no version, delivery time or acknowledgement record. Global and open-source tools do not handle it at all.
- **Builds on:** Onboarding checklists (§1.3), documents, the notification inbox, the employee invitation flow, the scheduled-change worker for re-issue dates.
- **Shape:** A built-in onboarding step that issues the current statement version and records the version, channel, time sent, time opened and acknowledgement. For casuals, reminders are scheduled at the points Fair Work specifies. A dashboard tile lists "Employees without a statement on record". When Fair Work publishes a new version, EMS flags who received the old one.

### Idea 3 — Right-to-Disconnect Guard
- **Problem:** Since August 2025 small-business employees may refuse unreasonable out-of-hours contact. Yet HR and rostering tools send notifications at any hour, which creates the very contact the law is about.
- **Who benefits:** Employees (quiet evenings) and employers (evidence that the system respects working hours).
- **Why competitors fall short:** Thinly served everywhere (matrix #40). It is usually left to a policy document. Deputy users already complain about notification overload.
- **Builds on:** `Pukar.Notifications` (one central place every notification passes through), shifts (actual working times), employee records.
- **Shape:** Each employee has working hours or "follow my roster", plus optional on-call periods. Every notification type carries an urgency level (routine, time-sensitive, emergency). Routine messages sent outside working hours are held and delivered at the start of the next shift or as a morning digest. Senders see "This will be delivered at 7:00 am Tuesday" before sending. Emergency overrides need a reason and are logged. A contact log report shows out-of-hours contact per employee, which is useful evidence if a dispute arises.

### Idea 4 — WHS Incident Register with Notifiable-Incident Clock
- **Problem:** Notifiable incidents must be reported to the regulator immediately, the site preserved and records kept for at least five years. Updated model laws (where a state or territory has adopted them) add reporting of 15 or more consecutive days of work-related absence. Small businesses usually rely on a paper book or a spreadsheet.
- **Who benefits:** Owners, supervisors and injured workers. Most relevant for hospitality, care, construction and agriculture.
- **Why competitors fall short:** Missing or partial across the whole matrix (#39). It is left to separate WHS tools that know nothing about leave or rosters.
- **Builds on:** Documents (photos, statements), tasks (corrective actions), leave records (absence linked to the incident), notifications, audit trail, retention dates.
- **Shape:** A quick incident form usable on a phone. A guided "Is this notifiable?" decision helper asks plain-English questions, gives its answer and the reasons, and is clear that it is a guide only. If notifiable, a visible clock starts and the regulator-notified fields are recorded (date, time, reference, person). Corrective actions become tasks. When linked leave reaches the extended-absence threshold, the system raises a prompt. Retention is set to at least five years, and the records cannot be deleted.

### Idea 5 — Agreements and Requests Register with Response Clocks
- **Problem:** Written agreements such as time off instead of overtime, annual leave cash-out, leave in advance and individual flexibility arrangements must be kept as records. Flexible work requests need a written response within 21 days, with reasons if refused. These usually live in email threads.
- **Who benefits:** Employers (they meet deadlines and keep records) and employees (they see the status of what they asked for).
- **Why competitors fall short:** Most products store these as generic documents, with no type, deadline or link to the leave or hours they affect.
- **Builds on:** Documents, tasks (response due), notifications, the leave module (cash-out and leave-in-advance affect balances), audit.
- **Shape:** Typed agreement records with start and end dates, the terms, signed copies, and a link to their effect (for example, a cash-out reduces the annual leave balance through the ledger in Idea 8). Requests have a response clock and a reminder before the deadline. Refusals require reasons. Everything is included in the inspector pack (Idea 1).

### Idea 6 — Confidential FDV Leave Mode
- **Problem:** Paid family and domestic violence leave must be recorded but must not appear on payslips. In practice, ordinary HR tools leak it through leave calendars, team views, notifications, exports and audit logs.
- **Who benefits:** Employees experiencing family and domestic violence, which is a safety issue, and employers who need to meet the payslip rule.
- **Why competitors fall short:** Vendors treat FDV as "just another leave type". Even products with the payslip rule rarely control every other place where the leave could show up.
- **Builds on:** Leave types (a new confidential flag), the RBAC capability model, the notification producer, the audit interceptor (needs field filtering, which is itself a known gap).
- **Shape:** A "confidential" flag on a leave type. Confidential leave shows as "Leave" (or as ordinary hours in the payroll export) on calendars, in team views, notifications and exports. Only a named confidential-leave officer can see the real type. Any access to the details requires a reason and is logged. The audit trail stores a masked value. A test pack proves there are no leaks, which also makes a strong portfolio story.

### Idea 7 — Compliance Health Check ("What's missing?")
- **Problem:** Owners don't know which records are incomplete until something goes wrong.
- **Who benefits:** Owners and office managers, and their advisers.
- **Why competitors fall short:** Suites show dashboards of activity, not dashboards of compliance gaps. Checks are scattered across modules, if they exist at all.
- **Builds on:** Employee records, onboarding progress, document expiry, leave balances, attendance, notifications.
- **Shape:** One page of plain-English checks, each with a count and a "fix it" link. Examples: "3 employees have no employment type", "2 casuals have no CEIS on record", "1 new starter has no super fund recorded before first pay", "4 licences expire within 30 days", "Accrual has not run for October", "5 timesheets are unapproved for last week". Each check explains why it matters and cites the obligation. There is no score to game, only a list.

### Idea 8 — Explainable Leave Ledger ("Why is my balance 61.3 hours?")
- **Problem:** Leave balances are a black box. Mistakes are found late and are hard to trace. Today EMS stores running totals that are changed in place, which is the root cause of its double-counting and repeat-accrual defects (C4, §1.7).
- **Who benefits:** Employees (trust), managers (no more disputes) and administrators (they can find and fix errors).
- **Why competitors fall short:** Market research identifies award and leave misconfiguration, and the underpayment that follows, as a recurring complaint (01 §8 row 3). Balances are shown as a single number.
- **Builds on:** Leave balances, accrual run, approval, cancellation refunds, import, audit.
- **Shape:** Every change to a balance becomes a dated ledger entry: opening, accrual for a period, leave taken, cancellation refund, carry-forward, adjustment with reason, cash-out. The balance is the sum of the entries. A "Why this balance?" view lists the entries in plain English ("+2.92 h accrued for 1–31 Oct at 38 h/week, pro rata 0.8"). Each accrual entry names the rule and its inputs. Because each period can be posted only once, repeated accrual runs do nothing by design. A "what-if" preview shows the balance after a requested leave period, including public holidays.

### Idea 9 — Human-Approved AI Action Drafts ("Draft Desk")
- **Problem:** AI agents that act directly are hard to trust in HR. Metered or opaque AI is becoming common (Personio credits, Workday Flex Credits).
- **Who benefits:** Managers and administrators who want help without handing over control.
- **Why competitors fall short:** Employment Hero and Deputy do have human-in-the-loop agents (matrix #35). They rarely show *why* the agent proposed something, which data it used, or a durable record of the decision.
- **Builds on:** Existing commands such as leave approval, scheduled changes, shift creation and tasks; RBAC; the audit trail; notifications.
- **Shape:** The AI never writes data. It creates a **draft action**: for example, "Approve Sam's leave 3–7 Dec: balance is enough, no roster clash, 1 other team member is away". Each draft shows the proposed change, the facts used (with links), the rules checked and anything uncertain. A person approves, edits or rejects it. Only then does the normal command run, under that person's permissions. Draft, inputs, model version and decision are all stored. The AI makes no judgements about people's performance or character. Every draft has a "show me the data" button.

### Idea 10 — Grounded Policy and Entitlements Q&A
- **Problem:** Staff ask the same questions ("How much personal leave do I have? Can I cash out leave? What's our uniform policy?") and managers answer from memory, sometimes wrongly.
- **Who benefits:** Employees, and managers who stop being the help desk.
- **Why competitors fall short:** AI Q&A is now standard, but answers often mix general internet knowledge with company policy and seldom show their sources or the asker's own data.
- **Builds on:** The documents and policy library, the leave ledger (Idea 8), the employee portal, RBAC (answers limited to what the asker may see).
- **Shape:** Answers draw only on the organisation's own uploaded policies, the asker's own records and a curated set of official Fair Work summaries. Every answer shows its sources ("From: Leave Policy v3, section 4; your ledger as at today"). When the answer is unclear it says so and offers "Ask my manager", which creates a task. Unanswered questions are logged so the owner can see which policies need improving.

### Idea 11 — Plain-English Reports and the Monday Briefing
- **Problem:** Reports are either rigid or need an analyst (Humanforce, OrangeHRM and BambooHR complaints). Owners want answers in sentences.
- **Who benefits:** Owners and managers.
- **Why competitors fall short:** Employment Hero has a natural-language report builder, but most products give charts without explaining what they mean.
- **Builds on:** Attendance analytics (punctuality, absenteeism, period summaries), exports, the manager team summary, the leave ledger.
- **Shape:** (a) Type a question and EMS maps it to one of a set of safe, pre-built report templates. It shows which template and filters it used, so the result can be checked and saved. (b) A weekly briefing written in plain English: "Leave liability rose 6% to 412 hours, mostly from 2 people with over 8 weeks banked. Late starts doubled at the Main St site on Saturdays." Each sentence links to its numbers. The briefing is delivered within working hours (Idea 3).

### Idea 12 — Manager "Today" Exceptions View
- **Problem:** Managers of small teams don't have time to visit five screens. They need to know what needs attention now.
- **Who benefits:** Frontline managers and owner-operators.
- **Why competitors fall short:** Deputy has AI "exception rundowns". Most HR suites do not. Where they exist, they cover rostering only, not documents, onboarding or leave.
- **Builds on:** The manager team dashboard (§1.9), leave approvals, attendance today, overdue tasks, upcoming scheduled changes, document expiry.
- **Shape:** One list ordered by urgency, built from plain rules (no AI needed): "Alex hasn't clocked in for the 9:00 shift", "Priya's First Aid certificate expires Friday", "2 leave requests are waiting, one starts tomorrow", "Jordan's transfer takes effect Monday". Each item has a one-tap action, and later can carry an AI draft (Idea 9).

### Idea 13 — Credential Wallet that Guards the Roster
- **Problem:** In care, disability support, hospitality, security and transport, staff must hold current checks and licences (for example Working with Children Check, NDIS worker screening, first aid, RSA, police check, driver's licence). Rostering someone whose check has expired is a serious compliance failure.
- **Who benefits:** Employers in regulated sectors, which are common in regional areas, and workers, who get reminders in time.
- **Why competitors fall short:** AU workforce tools track qualifications (matrix #28). Global and open-source tools treat them as documents with dates. Few explain the block ("can't roster: WWCC expired 3 Nov") in plain terms or let a worker renew from their phone.
- **Builds on:** Document types, issue and expiry dates, the `DocumentExpiryNotificationWorker`, shifts and overlap validation, job positions.
- **Shape:** Positions list the credentials they require. The shift scheduler checks them and either blocks or warns, explaining why. Employees upload renewals from the portal, and a manager verifies them. Reminders are sent at 60, 30 and 7 days and respect quiet hours. A report lists credential coverage by site.

### Idea 14 — Explainable Hours Calculation (without an award engine)
- **Problem:** Small businesses can't afford award-engine mistakes, and EMS shouldn't try to build one. They still need to see, for each shift, what counted as ordinary time, breaks and possible overtime, and why.
- **Who benefits:** Payroll officers, managers and employees who check their hours.
- **Why competitors fall short:** Award engines give an answer without a rationale, and vendors cap their liability when they get it wrong (Deputy review, 01 §3.2).
- **Builds on:** Attendance records, breaks, `AttendancePolicy` (standard daily minutes, overtime flag), shifts.
- **Shape:** A per-shift breakdown: rostered time vs actual, breaks taken vs required, ordinary and over-threshold minutes, with the rule shown in plain English. Warnings appear for things such as a missed meal break, a short gap between shifts or an unrostered clock-in. The approved hours are exported to payroll (Idea 15), which applies award rates. EMS's job is to make the *hours* defensible.

### Idea 15 — "HR around Xero" Payroll Bridge with Reconciliation
- **Problem:** Many AU micro-businesses run payroll in Xero but manage HR in spreadsheets. Rushed integrations cause reconciliation problems (Employment Hero complaint).
- **Who benefits:** Businesses already on Xero, and their bookkeepers.
- **Why competitors fall short:** Xero has no HR. Suites want to replace Xero payroll rather than sit beside it.
- **Builds on:** The integration outbox and dispatcher, approved leave, attendance and timesheets, employee records.
- **Shape:** After timesheet approval, push approved hours and leave to Xero Payroll. A reconciliation screen compares what EMS sent with what Xero holds and lists differences in plain English ("Xero shows 7.6 h annual leave for Kim on 2 Dec; EMS approved 3.8 h"). Super fund and stapled-fund status collected at onboarding are passed across, and anything missing is flagged before the first pay run (Payday Super). Confidential FDV leave is exported as ordinary hours (Idea 6).

### Idea 16 — Multilingual, Plain-Language Employee Experience
- **Problem:** Much of Australia's frontline workforce speaks English as an additional language. HR jargon causes errors and means people miss out on entitlements.
- **Who benefits:** Employees from migrant backgrounds, and employers who want instructions understood.
- **Why competitors fall short:** Vendor UIs usually support a few European languages. Rights information and notifications are typically English only.
- **Builds on:** The employee portal, notifications, onboarding (FWIS/CEIS delivery), the Next.js front end.
- **Shape:** Each employee chooses a preferred language for portal screens and notifications (records stay in English, as Fair Work requires). Labels are in plain English first (for example "Sick and carer's leave" alongside "Personal/carer's leave"). Rights statements are delivered in the employee's language wherever Fair Work publishes an official translation, with the English version attached. Optional AI translation of free-text messages is clearly marked as machine-translated.

### Idea 17 — Shift-Worker and Low-Connectivity Mode
- **Problem:** Regional sites and frontline workers have patchy mobile coverage, shared devices and little time to spare. Desktop-first HR tools fail them.
- **Who benefits:** Casual and shift workers, regional employers, and sites with a shared tablet.
- **Why competitors fall short:** Deputy and Tanda lead here, but users complain of slow apps and differences between mobile and desktop. Suites assume office workers.
- **Builds on:** Attendance (GPS check-in), the employee portal, shifts, notifications.
- **Shape:** An installable web app (PWA) with self check-in and check-out that queues offline and syncs later, keeping the original time and flagging "recorded offline". A kiosk mode for a shared tablet uses PIN sign-in and an optional photo. A large-button, one-screen "my next shift" view. SMS fallback for urgent roster changes, within quiet-hour rules (Idea 3).

### Idea 18 — Transparent Plans, No Seat Minimums
- **Problem:** Minimum seat counts and add-on creep make small teams pay two to three times the headline price (01 §9).
- **Who benefits:** Businesses with 5–15 staff in particular.
- **Why competitors fall short:** Employment Hero has a 10-user minimum. BambooHR has a flat fee for 25 or fewer staff. HiBob, Rippling and Humanforce are quote only.
- **Builds on:** The RBAC capability model and menu permissions (features can be switched on per plan).
- **Shape (portfolio demonstration, not a commercial offer):** A plan and feature-flag model in which every feature belongs to a published plan. A public "what will I pay" calculator with no hidden add-ons. An in-app "why can't I use this?" message that names the plan, rather than a hidden menu. AI features are included and capped, not metered by credits.

### Idea 19 — Open, Self-Hostable, "Your Data Leaves With You"
- **Problem:** Small businesses fear lock-in and offshore data handling (Tanda's overseas subprocessors are a noted complaint). Open-source HR products are dated and not built for Australia.
- **Who benefits:** Privacy-conscious businesses, IT providers who look after small clients, and developers reviewing the code.
- **Why competitors fall short:** Open-source tools lack AU compliance (01 §4). Australian suites are closed SaaS products.
- **Builds on:** Docker Compose, the REST API, exports, the outbox (a natural base for webhooks), and clean layered architecture.
- **Shape:** One-command self-hosting with an Australian data-residency option. Published OpenAPI documentation and outgoing webhooks for key events (employee created, leave approved, incident logged). A full data export in open formats (CSV/JSON plus documents) at any time, and an "exit pack" that matches the inspector pack (Idea 1). Plain statements of where data is stored and who can see it.

---

## 3. Foundation items (fix before any of the above can be credible)

These are the reviewer's Critical and High cons (03-project-review). Any product that claims to be compliance first must fix them first, because several ideas above (the inspector pack, confidential FDV leave, the leave ledger) depend on access control and data integrity being trustworthy.

| Ref | Severity | Foundation item (plain language) | Ideas that depend on it |
|---|---|---|---|
| C1 | Critical | Being logged in is enough to change most of the system. Role and ownership checks are missing on about half the API, and anyone can self-register. Self-service actions (such as check-in) must work out the employee from the login, not from the request | All, especially 1, 3, 4, 6, 7 |
| C2 | Critical | Any logged-in user can give the ADMIN role to a job position, and so to themselves | All |
| C3 | Critical | Employee documents and leave attachments can be downloaded without logging in | 1, 2, 4, 5, 6, 13 |
| C4 | Critical | Anyone can run leave accrual, and every run adds leave again. Balances have no concurrency protection | 8, 15, 6 |
| C5 | High | The ordinary self-service Leave permission lets staff view and manage everyone's leave, including medical certificates | 6, 8, 10 |
| C6 | High | Uploads accept dangerous file types that are then served from the API's own address | 2, 4, 5, 13 |
| C7 | High | No protection against password guessing (no rate limiting or lockout) | All |
| C8 | High | Tests are thin, partly placeholders, not run automatically, and at least one would fail. There is no CI | All (no proof the rules hold) |
| C9 | High | The one-command Docker setup does not build and would start with a publicly known admin password | 19 |
| C10 | High | Development secrets and a default admin password are committed to the repository | 19, all |

**Related defect from the system inventory (§1.7), in the same high-risk area as C4:** the leave-year reset double-counts carry-forward, so available balance after a reset is twice the amount carried. The leave ledger (Idea 8) removes the cause, but the defect is a foundation item in its own right.

---

*Not covered in this document, by design: feasibility, effort, sequencing and priority. Those belong to the Product Owner.*
