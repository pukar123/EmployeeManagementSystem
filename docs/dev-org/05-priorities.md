# 05 — Priorities: feasibility, cost and order of work

**Prepared by:** Product Owner (AI team)  **Date:** 15 November 2026
**Inputs:** `01-market-research.md`, `02-system-inventory.md`, `03-project-review.md` (cons C1–C23), `04-gaps-and-ideas.md` (gaps #1–#47, Ideas 1–19). Effort was spot-checked against the code at `/home/claude/work/ems`.
**Scope note:** EMS is a personal portfolio project. "Value" means value to EMS as a credible, explainable HR product and as evidence of good engineering for a hiring manager. Nothing here is a commercial plan.

---

## 1. How I scored

| Measure | Meaning |
|---|---|
| **Value (1–5)** | 5 = must have before EMS is shown to anyone, or a stand-out portfolio story. 1 = nice to have. |
| **Effort (daily runs)** | **S** = 1 run. **M** = 2–3 runs. **L** = 4–6 runs. **XL** = 7+ runs. One run is one feature-sized change (a few hundred to about 1,500 lines). |
| **Running cost** | **$0** = no new hosting or service. **Low** = free tier or a few dollars a month. **Paid** = needs a paid API or service (flagged, with a cheaper path). |
| **Risk** | Uses the DEV-ORG high-risk list. Anything touching money, payroll, security, sign-in, permissions, personal data, deletion, database migrations, leave balances or pay data is **High**. Docker, Redis and logging changes are **Medium**. |
| **Verdict** | **Do** (in the next 2–3 weeks), **Do later** (worth doing, but after something else), **Don't** (cut, with reason). |

### Two delivery constraints the CEO should know about

1. **Approval gates will be the bottleneck, not build capacity.** 14 of the 20 backlog tasks are High risk, so under the standard matrix each one pauses for CEO approval. With a review every 7–10 days, the team would finish one task and then wait a week. **Decision for the CEO (not changed by me):** either approve the security and leave batches as a plan up front and keep the gate at merge review, or expect the backlog to take 6–8 weeks rather than 3.
2. **Nothing has been compiled in the team's environment.** The reviewer had no .NET SDK, and the last feature was delivered uncompiled. Task B01 (an automatic build and test check on GitHub) is first for that reason. It needs the repository to be on GitHub with Actions enabled, which only the CEO can confirm. Until B01 is green, every later task is unverified.

---

## 2. Scoring table

### 2.1 Reviewer cons (all forwarded and all included)

| Ref | Item (plain language) | Value | Effort | Running cost | Risk | Depends on | Verdict |
|---|---|---|---|---|---|---|---|
| C1 | Half the API checks only that you are logged in; anyone can self-register | 5 | M (split across B04, B05, B07, B09) | $0 | High | B03 | **Do** — first thing a reviewer tests |
| C2 | Any user can give ADMIN to a job position | 5 | S | $0 | High | B03 | **Do** — full takeover risk, one-line class of fix |
| C3 | Employee documents and leave attachments downloadable without logging in | 5 | M | $0 | High | B04 | **Do** — personal information leak |
| C4 | Leave accrual can be run repeatedly by anyone, adding leave each time | 5 | M | $0 | High | B04, B11 | **Do** — leave balance integrity |
| C5 | Ordinary Leave permission lets staff manage everyone's leave | 5 | S | $0 | High | B03 | **Do** |
| C6 | Uploads accept dangerous file types | 4 | S–M | $0 | High | B08 | **Do** |
| C7 | No protection against password guessing | 4 | M (B05 rate limit, B13 lockout) | $0 | High | B03 | **Do** |
| C8 | Thin, partly fake, unrun tests; no CI | 5 | L (B01, B03, then tests in every task) | $0 (GitHub Actions free tier) | Low | — | **Do** — CI first; tests grow with each task |
| C9 | Docker one-command setup broken; known admin password | 4 | S | $0 | Medium–High | B01 | **Do** |
| C10 | Dev secrets and default admin password committed | 4 | S | $0 | High | — | **Do** (combined with C9 in B14) |
| C11 | Docs and rules contradict the code; Redis claimed but unused | 3 | S | $0 | Low | B14 | **Do** — remove Redis claims rather than add a cache |
| C12 | Errors handled by matching message text; inconsistent error shapes | 3 | M–L (99 catch blocks) | $0 | Medium | B03 | **Do later** — Medium tier; do one module at a time |
| C13 | Application layer depends on EF Core; DbContext in Domain | 3 | S for option (a), L for (b) | $0 | Low (a) / High (b) | — | **Do** option (a): document the trade-off and remove direct `AppDbContext` use in `LegacyRoleKeyGuard`. **Don't** do the full move (b) now — overengineering for the benefit |
| C14 | Login tokens kept in browser storage | 3 | M | $0 | High | B10, B15 | **Do later** — Medium tier; add a Content Security Policy first, move refresh token to an HttpOnly cookie after |
| C15 | Known-vulnerable frontend packages | 4 | S | $0 | Medium | B01 | **Do** |
| C16 | No caching, unpaged lists, N+1 loops | 2 | M | $0 | Low | — | **Do** the paging and accrual parts inside B09 and B12; **Don't** add Redis — in-memory cache later if needed |
| C17 | Last feature skipped the high-risk checks; hand-written migration | 5 | S | $0 | High | B01 | **Do** — HEAD may not build |
| C18 | Leftover migration-period scaffolding | 2 | M | $0 | High (auth scheme removal) | B03 | **Do later** — Medium tier, after the cutover is confirmed by the CEO |
| C19 | Two screens show raw database IDs | 2 | S | $0 | Low | — | **Do** (in B16) |
| C20 | Template leftovers and stray files | 2 | S | $0 | Low | — | **Do** (in B01) |
| C21 | Local Docker hardening | 2 | S | $0 | Medium | — | **Do** (in B14) |
| C22 | No error pages; very large components; few UI tests | 2 | M | $0 | Low | — | **Do later** — Low tier; error pages are a quick win to slip in |
| C23 | README undersells the project | 4 | S | $0 | Low | B01, B14 | **Do** (in B16) — screenshots need the CEO |

### 2.2 Known defects (from the system inventory)

| Ref | Item | Value | Effort | Cost | Risk | Depends on | Verdict |
|---|---|---|---|---|---|---|---|
| D1 | Leave-year reset counts carried-forward leave twice | 5 | S | $0 | High | B04 | **Do** (B11) |
| D2 | Accrual proration always uses calendar days in the month | 3 | S | $0 | High | B17 | **Do later** — fix when NES accrual is done |
| D3 | Leave amount is typed by the user; no working-day or public-holiday calculation | 4 | M | $0 | High | TS3 | **Do later** — Medium tier |
| D4 | Pages missing from the menu seed (portal, transfers, calendars) | 2 | S | $0 | High (permissions seed) | B04 | **Do later** — Medium tier |
| D5 | Leave docs say approval is "deferred" | 2 | S | $0 | Low | — | **Do** (in B16) |
| D6 | Sites are not tied to an organisation | 2 | S | $0 | High (migration) | — | **Do later** — Low tier, harmless while single-tenant |
| D7 | Docs describe predictable first passwords (`FirstName@123`); not found in current code | 3 | S | $0 | High | — | **Do** check in B16; correct docs or code |

### 2.3 Table-stakes gaps (from 04 §1)

| Ref | Item | Value | Effort | Cost | Risk | Depends on | Verdict |
|---|---|---|---|---|---|---|---|
| TS1 | Employment basis on the record: full-time, part-time, casual; standard weekly hours (#1) | 4 | S–M | $0 | High (personal data, migration) | B03 | **Do** — Medium tier first item; NES leave depends on it |
| TS2 | Employee self check-in/out from the portal (#4, #14) | 4 | S | $0 | Medium | B07 | **Do** — Medium tier |
| TS3 | NES leave types seeded, public holidays by state, working-day leave calculation (#12, #13) | 5 | M | $0 (public holiday data from data.gov.au CSV, imported by admin) | High | TS1, B17 | **Do** — Medium tier |
| TS4 | Readable audit trail with sensitive fields masked (#33) | 4 | M | $0 | High (personal data) | B04 | **Do** — Medium tier |
| TS5 | Email notifications (#1.13) | 3 | S–M | Low (SMTP already exists; Mailpit for dev, any free SMTP tier) | Medium | — | **Do** — Medium tier |
| TS6 | Timesheet approval (#15) | 4 | M | $0 | High (pay-related hours) | TS2 | **Do later** — Medium tier, after self check-in |
| TS7 | Leave liability and headcount reports (#32) | 3 | M | $0 | Medium | B17 | **Do later** — Medium tier |
| TS8 | Offboarding checklist reusing onboarding templates (#8) | 3 | S–M | $0 | Medium | — | **Do later** — Medium tier |
| TS9 | Org chart view (#3) | 2 | S | $0 | Low | — | **Do later** — Low tier, cheap demo item |
| TS10 | Policy library with read-and-acknowledge (#9, #10) | 3 | M | $0 | Medium | B08, B10 | **Do later** — Low tier |
| TS11 | Installable web app (PWA) for the portal (#5) | 2 | S | $0 | Low | TS2 | **Do later** — Low tier |
| TS12 | Payroll export file (Xero-compatible CSV) (#24) | 3 | M | $0 | High (pay data) | TS6 | **Do later** — Low tier (cheap path to Idea 15) |
| TS13 | Super fund details captured for payroll (#23) | 2 | S | $0 | High (pay/personal data) | TS12 | **Do later** — Low tier, only with payroll export |
| TS14 | Basic roster templates and publish step (#17) | 2 | M | $0 | Low | — | **Do later** — Low tier |
| TS15 | AI Q&A assistant (#34) | 3 | see Idea 10 | Paid | Medium | — | see Idea 10 |

### 2.4 The 19 ideas

| Idea | Item | Value | Effort | Running cost | Risk | Depends on | Verdict |
|---|---|---|---|---|---|---|---|
| 1 | Fair Work inspector-ready records pack | 4 | L | $0 | High (personal data export) | Idea 8, TS4, Idea 5 | **Do later** — Low tier; strong idea but needs the ledger and a readable audit first |
| 2 | FWIS/CEIS proof of delivery | 3 | M | $0 (Fair Work PDFs linked, not hosted) | Medium | TS1, TS5 | **Do later** — Medium tier |
| 3 | Right-to-disconnect notification guard | 4 | M | $0 | Medium | TS5 | **Do later** — Medium tier; only meaningful once email exists |
| 4 | WHS incident register with notifiable clock | 3 | L | $0 | High (health info) | B08, B10 | **Do later** — Low tier |
| 5 | Agreements and requests register with response clocks | 2 | M–L | $0 | High (affects leave balances) | Idea 8 | **Do later** — Low tier |
| 6 | Confidential FDV leave mode | 4 | L | $0 | High (safety, permissions, audit masking) | TS4, B06 | **Do later** — Low tier; excellent story but needs field-level masking first, and getting it wrong is a safety issue |
| 7 | Compliance health check ("What's missing?") | 5 | S per batch of checks | $0 | Low (read-only) | B04 | **Do** — High tier; cheap, rule-based, very demoable |
| 8 | Explainable leave ledger ("Why is my balance 61.3 hours?") | 5 | L (3 runs in backlog) | $0 | High | B11, B12 | **Do** — High tier; removes the root cause of C4/D1 and is the best interview story |
| 9 | Human-approved AI action drafts | 3 | XL | **Paid** (LLM API) | High | Idea 12, TS4 | **Do later** — Low tier. Cheap path: build the non-AI version first (Idea 12), then add a provider interface with a capped budget or a local model (Ollama) |
| 10 | Grounded policy and entitlements Q&A | 3 | L | **Paid** (LLM API + embeddings) | Medium | TS10, Idea 8 | **Do later** — Low tier. Cheap path: SQL full-text search over policies first, then a small hosted model with a hard monthly cap (for example a few dollars), or a local model for demos |
| 11 | Plain-English reports and Monday briefing | 3 | M (briefing) / XL (natural-language builder) | $0 for template sentences; Paid for natural-language queries | Low | TS7, TS5 | **Do later** — Low tier, template-sentence briefing only. **Don't** build the natural-language report builder |
| 12 | Manager "Today" exceptions view | 4 | S–M | $0 | Low (read-only, scoped) | B07 | **Do** — Medium tier; rules only, reuses manager dashboard |
| 13 | Credential wallet that guards the roster | 4 | M | $0 | Medium | B09, B10 | **Do later** — Medium tier; strong fit for care-sector employers |
| 14 | Explainable hours calculation (no award engine) | 3 | M | $0 | High (pay-related) | TS6 | **Do later** — Low tier |
| 15 | Xero payroll bridge with reconciliation | 3 | XL | Low (free Xero developer account and demo company) | High (pay data, OAuth) | TS6, TS12 | **Do later** — Low tier; start with the CSV export (TS12) |
| 16 | Multilingual, plain-language employee experience | 2 | L | $0 for UI translations; Paid for AI translation | Low | — | **Don't** for now — plain-English labels can go into normal work; full i18n is large for little portfolio value |
| 17 | Shift-worker and low-connectivity mode | 2 | XL (offline sync, kiosk, SMS) | **Paid** for SMS | Medium | TS2, TS11 | **Don't** as specified — offline sync and kiosk are large and error-prone. Keep only the PWA (TS11). No SMS |
| 18 | Transparent plans, no seat minimums | 1 | M | $0 | Medium (permissions) | — | **Don't** — a pricing model adds complexity and makes a portfolio project look like a commercial product |
| 19 | Open, self-hostable, "your data leaves with you" | 3 | S (Docker + OpenAPI) / L (webhooks, exit pack) | $0 | Medium | B14 | **Do** the cheap part (working Docker, B14; OpenAPI in all environments later). **Do later** webhooks and exit pack |

### 2.5 Other gaps from the market matrix

| Item | Verdict |
|---|---|
| Custom fields, performance reviews, goals, learning, recognition, surveys, assets, expenses, recruitment, compensation, demand forecasting, native payroll, STP lodgement, award engine, configurable workflow engine, SSO/SCIM | **Don't** — outside the target and the BA marked them Skip. Award engine and STP stay with payroll systems |
| MFA (authenticator app codes) | **Do later** — Low tier, $0, High risk |
| 7-year retention purge/anonymise job (#41) | **Do later** — Low tier; deletion is High risk and needs the audit viewer first |
| Shift swap, open shifts, availability (#18) | **Don't** for now |

---

## 3. Priority tiers

### High priority — incorporate immediately (backlog B01–B20)
Security foundations and defects first, then the few things that most raise competitiveness.
1. Build and test check on every change, plus repair of the uncompiled last feature (C8, C17, C20)
2. Integration test harness for "who can do what" (C8)
3. Close the privilege escalation and lock admin-only setup screens (C2, C1)
4. Close public self-registration and slow down password guessing (C1, C7)
5. Fix the Leave permission leak (C5)
6. Make attendance safe: you can only clock yourself in unless you are an attendance admin or the manager (C1)
7. Private storage and authorised download for documents and attachments; document access checks (C3, C1)
8. Safe upload rules (C6)
9. Fix the leave-year reset double count (D1)
10. Accrual can only run once per period; balance updates protected from collisions (C4)
11. Account lockout after repeated failed logins (C7)
12. Secrets out of the repository; Docker that works with one command (C9, C10, C21)
13. Frontend security upgrades and automatic dependency and secret scanning (C15, C10)
14. Honest docs and README refresh (C11, C13a, C19, C23, D5, D7)
15. **Explainable leave ledger** (Idea 8) — ledger, "why this balance" view, adjustments with reasons
16. **Compliance health check** (Idea 7)

### Medium priority (next 2–4 weeks, roughly in this order)
1. Employment basis and standard hours on the employee record (TS1)
2. Employee self check-in/out in the portal (TS2)
3. Manager "Today" exceptions view (Idea 12)
4. NES leave types, state public holidays and working-day leave calculation (TS3, D2, D3)
5. Readable, masked audit trail (TS4)
6. Email notifications (TS5)
7. Consistent error responses, one module at a time (C12)
8. Content Security Policy, then refresh token in an HttpOnly cookie (C14)
9. Remove migration-period scaffolding once the CEO confirms the cutover (C18)
10. Right-to-disconnect notification guard (Idea 3)
11. FWIS/CEIS proof of delivery (Idea 2)
12. Credential wallet that guards the roster (Idea 13)
13. Timesheet approval (TS6)
14. Leave liability and headcount reports (TS7)
15. Offboarding checklist (TS8)
16. Missing pages in the menu seed (D4)

### Low priority
- Inspector-ready records pack (Idea 1)
- Confidential FDV leave mode (Idea 6)
- WHS incident register (Idea 4)
- Agreements and requests register (Idea 5)
- Explainable hours calculation (Idea 14)
- Payroll CSV export, then Xero bridge (TS12, Idea 15) and super fund capture (TS13)
- Monday briefing in template sentences (Idea 11, part)
- Policy library with acknowledgement (TS10), then grounded Q&A (Idea 10, capped cost)
- AI action drafts (Idea 9, capped cost)
- Webhooks and data exit pack (Idea 19, part)
- Org chart (TS9), PWA (TS11), roster templates (TS14)
- Frontend error pages and component tests (C22)
- MFA; retention purge job; Sites tied to organisation (D6)

### Not doing
| Item | Reason |
|---|---|
| Transparent plans / seat pricing (Idea 18) | Makes a personal portfolio project look commercial and adds a permissions layer for no user benefit |
| Offline sync, kiosk mode and SMS (Idea 17, most of it) | Large, error-prone, and SMS is a paid service. The PWA alone covers the portfolio point |
| Full multilingual experience and AI translation (Idea 16) | Large effort, small portfolio value; plain-English labels can be done in normal work |
| Natural-language report builder (Idea 11, part) | Paid AI and hard to make correct; saved report templates give most of the value |
| Redis caching (C11, C16) | No measured need. Remove Redis from the stack claims; use in-memory caching if a hotspot appears |
| Moving DbContext and migrations out of Domain (C13 option b) | Large, High-risk refactor for an architecture purity point; documenting the trade-off honestly is the better interview answer |
| Award engine, STP lodgement, native payroll, global payroll/EOR | Market advice is clear: integrate, don't build |
| Performance, goals, learning, recognition, surveys, recruitment, assets, expenses, compensation, custom fields, SSO/SCIM, demand forecasting, workflow engine | Outside the 5–50 staff, compliance-first target |
| Shift swaps, open shifts, availability | Rostering depth is not where EMS can stand out |

---

## 4. Rationale

- **Security first, without exception.** EMS is an HR system holding identity documents and medical certificates. C1–C7 are the first things a technical reviewer will try, and several are one-afternoon fixes. Showing EMS with these open would cost more credibility than any feature gains.
- **Build proof before more code.** The last feature was delivered without compiling. B01 makes every later task verifiable. B03 gives every security task a place to add a "this user gets 403" test, so the fixes stay fixed.
- **Leave balances: fix, then make explainable.** B11 and B12 stop the immediate damage. The ledger (B17–B19) then removes the cause: a balance becomes the sum of dated entries, each run can post a period once, and anyone can see why the number is what it is. This uses EMS's existing strengths (audit, scheduled changes) and is a better story than any new module.
- **The health check is the cheapest competitive feature.** It is read-only, uses data EMS already holds, needs no AI and no paid service, and demonstrates the "compliance-first, explainable" position in one screen. It grows as later features add checks.
- **Ideas were cut or deferred when they depended on missing foundations** (FDV mode needs masked audit; inspector pack needs ledger and audit; AI needs a capped budget and a non-AI version first) or when they looked commercial (pricing plans).
- **Running cost stays at $0** for everything in the backlog. GitHub Actions, Dependabot and Gitleaks are free. The only paid items (Ideas 9, 10, 11-NL, 17-SMS) are Low priority or cut, and each has a capped or local-model path.
- **Effort reality check.** 20 tasks fit 20 daily runs, but 14 are High risk. The real pace depends on the CEO's approval decision in section 1.
