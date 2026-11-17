# 01 — Market Research: Employee Management / HRIS / Workforce Products

**Prepared by:** Market Researcher (Business Analysis function)
**Date:** 15 November 2026
**Purpose:** Inform the EMS product strategy. EMS is a personal portfolio project, built by a solo .NET/full-stack developer (ASP.NET Core, EF Core/SQL Server, Next.js). This report looks at what popular products do so that EMS can be positioned credibly against them. It is not a commercial plan.

**How to read this report**
- Prices are indicative. They come from vendor pages and third-party pricing guides, and the date checked is given wherever the source gave one. Several sources are published by competitors, which is flagged where it matters.
- Currency is USD unless marked AUD or EUR.
- The feature matrix (section 5) combines vendor pages and review sites with researcher judgement. Treat it as directional, not audited.
- Nothing here is legal or payroll advice. The compliance section summarises official Fair Work, ATO and Safe Work Australia guidance for product-design purposes.

---

## 1. Executive summary

1. **The market splits three ways.** Global HRIS suites (BambooHR, HiBob, Personio, Rippling, Workday) are built around the employee record. Australian workforce-management tools (Deputy, Tanda/Workforce.com, Humanforce) are built around the roster and the timesheet. Employment Hero is the Australian all-in-one that bridges both. Open source (OrangeHRM, Frappe HR, Odoo) competes on cost and control but lags on Australian compliance and UX.
2. **Australian award compliance decides who wins locally.** Every serious AU product sells award interpretation (modern awards, penalty rates, overtime). Global HRIS products mostly do not cover it and push payroll to local partners.
3. **AI moved from chat to action during 2025–2026.** Assistants now *do* things with a human confirming: approve leave, build rosters, draft reports, screen candidates (Employment Hero Hero AI, Deputy AI, Rippling AI, Workday agents). Vendors are also starting to meter AI usage (Workday Flex Credits; Personio AI Credits from August 2026).
4. **Pricing is polarised.** AU rostering costs about AUD 5–13 per user per month. HR suites cost about USD/AUD 10–25 per employee per month, but minimums (Employment Hero: 10 users; BambooHR: about USD 250/month flat for 25 or fewer staff) and paid add-ons push small teams well above the headline rate. Enterprise vendors (Workday, HiBob, Rippling) are quote-only.
5. **The recurring complaints are consistent across vendors:** opaque pricing and add-on creep, minimum charges that penalise very small teams, hard-to-reach support, complex award setup where a mistake causes underpayment, weak or rigid reporting, and mobile/desktop inconsistency.
6. **The opportunity for a small, focused product** is a transparent, explainable "compliance-first core HR + time" product for AU small businesses (5–50 staff). It would make NES leave, record keeping, the Fair Work Information Statement, right to disconnect and audit trails visible and provable. It would treat payroll/STP as an integration rather than a build, and use AI only for explainable, human-approved actions.

---

## 2. Global HRIS / HCM products

### 2.1 BambooHR
- **Target customer:** SMB to lower mid-market. Cites 30,000+ customers. Teams of 25 or fewer pay a flat monthly fee.
- **Core modules:** Employee database, self-service, time-off, reporting, onboarding/offboarding, e-signatures, workflows (Pro), performance (Pro+), comp planning and benchmarking (Elite). ATS is included with caps on open roles. Payroll and benefits are US-only add-ons.
- **Standout:** A very approachable UX. Elite includes salary benchmarking based on Mercer data.
- **AI (2025–26):** An AI assistant answers HR questions on every plan. Pro adds reading of policies and handbooks, and Elite adds benchmark analysis.
- **Pricing (checked Oct 2026):** Core about $10, Pro about $17, Elite about $25 per employee per month for teams over 25. For 25 or fewer staff, flat fees are $250, $425 and $650 per month. Applicant tracking, time tracking and implementation can cost extra.
- **Common complaints:** Add-on costs push the bill above the quote. The flat minimum makes very small teams expensive (about $25 per head at 10 staff). Moving up a tier is a big jump. Payroll is US-only. Core plans lack analytical insight.

### 2.2 Rippling
- **Target customer:** 1 to 1,000+ employees, especially US tech and venture-backed firms that want HR, IT and spend tools in one system.
- **Core modules:** HCM (HRIS, time, ATS, performance, learning, scheduling), payroll (including global payroll and EOR), IT (identity, devices, inventory) and spend (expenses, cards).
- **Standout:** The employee record drives identity, device and app provisioning. Its workflow and automation engine is strong.
- **AI:** Rippling AI works within existing user permissions and waits for human sign-off before acting. Automations run on triggers such as a schedule or a data change.
- **Pricing:** Quote-based and modular. Indicative figures are about $35–40 per month base plus about $8 per employee per month (Gusto-published guide, June 2026; another guide, October 2026, says HCM starts at about $8 per employee per month).
- **Common complaints:** No public rate card, separately priced modules, implementation and support upgrades at extra cost, and contract terms that vary from customer to customer.

### 2.3 HiBob (Bob)
- **Target customer:** Mid-market, roughly 50–3,000 employees, often multinational. Cites 5,500+ customers in 170+ countries.
- **Core modules:** Core (employee database, documents, time off, lifecycle workflows, analytics, mobile) plus Talent, Payroll & Benefits, and Planning & Compensation modules.
- **Standout:** Culture and engagement tools (shoutouts, clubs, social feed) and comp-review guardrails. Reviewers praise the UX and mobile app.
- **AI:** Bob AI Companion is included in Core. It drafts review summaries and survey themes, and HiBob also offers agents, MCP and Studio. HiBob markets a "glass box" approach with audit logs, says the AI never evaluates people, and cites ISO 42001 certification.
- **Pricing:** Quote-only. Estimates are about $5–10 per employee per month for Core and about $16–25 with modules. Implementation reportedly costs 10–20% of first-year contract value.
- **Common complaints:** Opaque pricing and no free trial. Renewal increases of about 20% are reported. Native payroll covers only the US and UK. The ATS is more manual than demos suggest. Cross-module reports need a lot of setup.

### 2.4 Personio
- **Target customer:** European SMB and mid-market.
- **Core modules:** Core / Core Pro (HR records, absence, documents, e-signature caps, API on Pro). Payroll, Recruiting, Performance, Compensation, Surveys and Whistleblowing are separate apps.
- **AI:** The Personio Assistant replaced the paid Conversations app on 30 June 2026 at no extra cost. **AI Credits launched on 5 August 2026**, Personio's first metered billing dimension. Billable AI features such as the Talent Screening Agent consume credits at €0.01 each, and prepaid packs followed on 9 October 2026.
- **Pricing:** Quote-only since mid-2023. The last published rates were about €2.88 (Essential) and €4.43 (Professional) per employee per month.
- **Relevance to AU:** Low. Mainly useful as a signal of where AI pricing is heading.

### 2.5 Workday HCM (light touch)
- **Target customer:** Enterprise and large public-sector organisations (1,000+ employees).
- **AI:** The Illuminate agents announced in September 2025 for 2026 delivery include Business Process Copilot, Case, Employee Sentiment, Job Architecture and Performance agents. **Flex Credits** are a consumption allowance included with every subscription, with top-ups available to buy.
- **Pricing:** Not published.
- **Relevance:** Sets the "agentic HR" narrative but is not a direct competitor to EMS.

### 2.6 Zoho People
- **Target customer:** SMBs, especially those already using Zoho. Quotes apply above 500 users.
- **Core modules:** Core HR, onboarding/offboarding, documents, surveys, HR help desk, time off, timesheets, attendance, OKRs/performance and LMS, depending on tier. Payroll sits outside the core plans, in the People Plus bundle.
- **AI:** Zia, an AI assistant/agent, is included from the Essential HR plan.
- **Pricing (both sources checked 26 Oct 2026, and they conflict):** One lists about $1.25–4.50 per user per month (annual billing). The other lists Essential HR $3, Workforce $5, Talent $5 and Enterprise $8 per user per month (annual), with a free plan for up to 3 users. Either way it is the cheapest commercial suite.
- **Common complaints:** Free-plan limits, setup and customisation that may need paid partners, and analytics that need a separate Zoho Analytics subscription.

### 2.7 Gusto
- **Target customer:** US small businesses. Its strength is bundling payroll with benefits.
- **Pricing:** Published and month-to-month. Simple is $49 base plus $6 per person, Plus is $80 plus $12, and Premium is $180 plus $22.
- **AI:** The Gus AI assistant handles small-business questions and reporting. Gusto also announced agents for accounting firms and payroll data access from inside ChatGPT. Detail was not verified because the vendor page failed to load.
- **Relevance to AU:** Low, as it is US-only payroll. Its **transparent, published pricing** is a model worth copying.

### 2.8 Deel
- **Target customer:** Companies with global or distributed teams that hire across 150+ countries through contractors, EOR or direct employment.
- **Core modules:** HR Core/Advanced, global payroll, contractor management, EOR, compliance, plus recent IT/device additions.
- **AI:** Drafts job descriptions and policies and answers questions from company data. The "AI Workforce" hub hosts agents such as a time-off request agent. Deel also offers a connector that exposes hiring, payroll and compliance data to external AI assistants, with permission-controlled actions.
- **Pricing:** HR Core about $5 and Advanced about $19 per employee per month. EOR is about $599 per employee per month.
- **Common complaints:** HRIS depth is limited compared with specialist tools, onboarding is less intuitive, and customisation is limited.

---

## 3. Australian market (likely first users)

### 3.1 Employment Hero (including the former KeyPay)
- **Target customer:** AU/NZ/UK/SG SMBs. It is strongest with growing, award-covered businesses in hospitality, retail, health and cleaning.
- **Core modules:** HR (records, documents, onboarding/offboarding, policies, performance, learning, recruitment, expenses), payroll (the former KeyPay engine; KeyPay was acquired in December 2021 and rebranded on 6 March 2023), rostering, time and attendance, and the Swag employee app.
- **AI (2026 product recap):**
  - Hero AI chat can approve or reject leave, create leave requests and act on timesheets from mobile.
  - AI Insights appear above charts in 12 standard report groups, and the Report Builder turns a plain-English description into a custom report.
  - **Conversational Rostering** handles open shifts, swaps and compliance checks through chat.
  - The **AI Payroll Agent** checks timesheets, leave and expenses against policy before payroll.
  - The Recruitment Agent includes AI video interviews, an AI notetaker and transparent hiring criteria.
  - Other 2026 additions include a Casual Conversion Pre-Check and Payday Super support through HeroClear (live from 1 July 2026).
- **Pricing (AU pricing page, Nov 2026):**
  - HR Essentials about $11 and HR Engage about $15. Elite and Unlimited are custom-priced.
  - Minimum of 10 users.
  - Add-ons per employee per month: rostering and time & attendance $5 (min $50), chat $2.50, HR Advisory $15, managed payroll $21.
  - A partner estimates a realistic $15–30+ per employee per month for small award-covered teams.
- **Common complaints:**
  - Hard to reach a human for support (the most common theme).
  - The 10-user minimum and contract terms.
  - Setup complexity, where award misconfiguration risks underpayment.
  - Add-on cost creep.
  - Xero/MYOB reconciliation problems if integration is set up in a hurry.
  - Ratings: Capterra AU 4.4/5 (239 reviews), Trustpilot 3.9, Xero App Store AU 3.2 (20 reviews).

### 3.2 Deputy
- **Target customer:** Shift-based SMBs such as hospitality, retail and healthcare. Sydney-headquartered.
- **Core modules:** Rostering, time and attendance (kiosk, mobile, GPS), leave, award pay-rate library and advanced award interpretation (Core+), demand forecasting, and an HR add-on. Timesheets export to Xero, MYOB, Employment Hero and KeyPay, so payroll stays elsewhere.
- **AI:** **Deputy AI** is an action agent for managers. It builds or edits schedules, finds replacements, handles sick cover, works by voice or text on mobile, sends daily exception rundowns and analyses Kudos recognition themes. It is human-in-the-loop: nothing is published or approved without confirmation.
- **Pricing (AUD per user per month, ex GST):** Lite about $6.75, Core $8.75, Pro $13, $30 monthly minimum (March 2026 data). Capterra lists lower figures, likely older or in another currency.
- **Common complaints:** Mobile/desktop inconsistency, hard-to-manage notifications, slow app loading, limited Xero integration, and missing industry-specific features. One serious (unverified) review alleges wrong award rules were sent to MYOB, causing underpayment, and that compensation was capped by the terms. Capterra AU rating 4.6/5 (775 reviews).

### 3.3 Tanda / Workforce.com
- **Target customer:** Multi-site AU hospitality, aged care, retail and pharmacy.
- **Core modules:** Workforce management (rostering, time, leave), HR (onboarding, contracts, qualifications), and payroll (STP Phase 2, Beam super, per vendor). Clock-in by app, kiosk, PIN, photo or GPS. Demand rostering can draw on POS data (Lightspeed).
- **Award interpretation:** 30+ vendor-maintained templates (including SCHADS and Aged Care). Not Fair Work certified, which is true of every vendor.
- **Pricing:** All-in-One (HR + payroll + WFM) about AUD 12.80 per user per month ex GST (checked Sep 2026). Individual modules are quote-only.
- **Common complaints:** No public WFM-only price, per-user billing that is costly for large casual workforces, overseas subprocessors, confusing branding, and unverified phone support.

### 3.4 Xero Payroll (HR touchpoints)
- **Target customer:** Very small AU businesses already on Xero.
- **Scope:** Payroll, STP, super, leave and timesheets, with employee self-service through the Xero Me app. It has **no built-in HR**, and modern award setup is manual.
- **Pricing:** Included with the Xero Growing plan (about AUD 42/month, May 2026).
- **Relevance:** A key **integration target**. Many AU micro-businesses run payroll in Xero and need "HR around it".

### 3.5 Humanforce
- **Target customer:** Large frontline and shift-based workforces (healthcare, hospitality, retail, aged care). It has expanded into HR through its acquisitions of intelliHR and LiveHire.
- **Core modules:** Scheduling, time and attendance (mobile, web, kiosk), award interpretation, leave, HR, payroll, self-service, analytics, an open API and the Integration Central hub.
- **AI:** An AI-powered Roster Manager with demand forecasting, compliant shift patterns and auto-fill.
- **Pricing:** Quote-only.
- **Common complaints:** Reports with limited customisation and export, slow performance at peak times, a dated back-office UI, few templated letters, and limited CSV import.

---

## 4. Open source / self-hosted

| Product | Licence / stack | Modules | Pricing | Strengths | Weaknesses |
|---|---|---|---|---|---|
| **OrangeHRM** | Starter (open source); Advanced (commercial, cloud) | Core HR, leave, time, recruitment, performance (on the free plan), mobile | Starter free; Advanced quote per user | Well known, large community, extensible, Capterra UK 4.2/5 | Complex leave-policy setup, limited native reporting, dated UI, many AI features cloud-only |
| **Frappe HR** | Open source (GPL family; built on the Frappe framework / ERPNext) | 13+ modules: lifecycle, leave, attendance with geolocation, expenses/advances, performance (KRAs), payroll and tax, mobile app | Self-host free; Frappe Cloud from about $16/month per site | Integrated HR + payroll + accounting, active development | Best value only inside the ERPNext ecosystem; payroll rules are generic, not AU awards |
| **Odoo HR** | Community LGPLv3 (Python/PostgreSQL); Enterprise commercial | Employees, time off, attendance, recruitment, appraisals, expenses, payroll (Enterprise) | AU: Standard about AUD 34–43/user/month, Custom about AUD 64–80 (Oct 2026); light self-service users about AUD 12.90 | Highly modular, wide ERP reach | Needs implementation partners; Community lacks key pieces (mobile, full accounting); per-user pricing is high for HR-only use |

**Takeaway:** None of the open-source options ships credible **Australian award / NES / STP** compliance out of the box. That is the gap a locally focused product can fill. Their UX is also generally dated compared with Deputy or HiBob.

---

## 5. Feature matrix (~40 capabilities)

Legend: **✓** = native and mature · **◐** = partial, basic, or paid add-on/higher tier · **✗** = not offered natively (may integrate) · **I** = via integration/partner only

Columns: BHR = BambooHR · RIP = Rippling · HIB = HiBob · PER = Personio · ZOH = Zoho People · GUS = Gusto · DEEL = Deel · EH = Employment Hero · DEP = Deputy · TAN = Tanda/Workforce.com · XRO = Xero Payroll · HF = Humanforce · ORG = OrangeHRM · FRA = Frappe HR · ODO = Odoo HR. (Workday is left out because it is enterprise-only and has every row at ✓.)

| # | Capability | BHR | RIP | HIB | PER | ZOH | GUS | DEEL | EH | DEP | TAN | XRO | HF | ORG | FRA | ODO |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Employee records / core HR | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ◐ | ◐ | ◐ | ✓ | ✓ | ✓ | ✓ |
| 2 | Custom fields / configurable profile | ✓ | ✓ | ✓ | ✓ | ✓ | ◐ | ◐ | ✓ | ◐ | ◐ | ✗ | ✓ | ◐ | ✓ | ✓ |
| 3 | Org chart | ✓ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✓ | ✗ | ✗ | ✗ | ◐ | ◐ | ✓ | ✓ |
| 4 | Employee self-service (web) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✓ | ✓ | ✓ |
| 5 | Native mobile app | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ◐ |
| 6 | Onboarding workflows / checklists | ◐ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✗ | ✓ | ◐ | ✓ | ✓ |
| 7 | Fair Work Information Statement / CEIS delivery in onboarding | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ◐ | ✓ | ◐ | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ |
| 8 | Offboarding (checklists, exit, final access) | ◐ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✓ | ✗ | ◐ | ◐ | ◐ | ◐ | ✓ | ◐ |
| 9 | Documents / policy library | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✗ | ✓ | ◐ | ✓ | ✓ |
| 10 | E-signature | ◐ | ✓ | ✓ | ◐ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✗ | ✓ | ✗ | ◐ | ◐ |
| 11 | Leave requests and approvals | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 12 | NES-correct leave types and accrual (AL, personal/carer's, compassionate, paid FDV, LSL) | ✗ | I | I | ✗ | ◐ | ✗ | ◐ | ✓ | ◐ | ✓ | ✓ | ✓ | ◐ | ◐ | ◐ |
| 13 | Public holiday calendars (by state) | ◐ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✓ |
| 14 | Attendance / clock in–out (GPS, kiosk) | ◐ | ✓ | ◐ | ◐ | ✓ | ◐ | ◐ | ◐ | ✓ | ✓ | ◐ | ✓ | ◐ | ✓ | ✓ |
| 15 | Timesheets and approval | ◐ | ✓ | ◐ | ◐ | ✓ | ✓ | ◐ | ◐ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 16 | Breaks / fatigue / meal-break rules | ✗ | ◐ | ✗ | ✗ | ◐ | ✗ | ✗ | ◐ | ✓ | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ |
| 17 | Rostering / shift scheduling | ✗ | ◐ | ✗ | ◐ | ◐ | ✗ | ◐ | ◐ | ✓ | ✓ | ✗ | ✓ | ✗ | ◐ | ◐ |
| 18 | Shift swap / open shifts / availability | ✗ | ◐ | ✗ | ✗ | ✗ | ✗ | ✗ | ◐ | ✓ | ✓ | ✗ | ✓ | ✗ | ◐ | ◐ |
| 19 | Demand forecasting for rosters | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ◐ | ✓ | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ |
| 20 | Modern award interpretation / penalty rates | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | ✓ | ◐ | ✓ | ✗ | ✗ | ✗ |
| 21 | Payroll (native) | I | ✓ | ◐ | ◐ | ◐ | ✓ | ✓ | ✓ | ◐ | ✓ | ✓ | ✓ | ✗ | ✓ | ◐ |
| 22 | Single Touch Payroll (AU) | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ◐ | ✓ | I | ✓ | ✓ | ✓ | ✗ | ✗ | ◐ |
| 23 | Super / Payday Super handling (AU) | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ◐ | ✓ | I | ✓ | ✓ | ✓ | ✗ | ✗ | ◐ |
| 24 | Payroll / accounting integration (Xero, MYOB, KeyPay) | ◐ | ◐ | ◐ | ✗ | ◐ | ◐ | ◐ | ✓ | ✓ | ✓ | — | ✓ | ✗ | ✗ | ◐ |
| 25 | Performance reviews | ◐ | ✓ | ✓ | ◐ | ◐ | ✗ | ◐ | ✓ | ✗ | ✗ | ✗ | ✓ | ✓ | ✓ | ✓ |
| 26 | Goals / OKRs | ◐ | ✓ | ✓ | ◐ | ◐ | ✗ | ◐ | ✓ | ✗ | ✗ | ✗ | ✓ | ◐ | ✓ | ◐ |
| 27 | Learning / training (LMS) | ✗ | ◐ | ◐ | ✗ | ◐ | ✗ | ◐ | ◐ | ◐ | ◐ | ✗ | ◐ | ✗ | ◐ | ◐ |
| 28 | Qualifications / licences / expiry tracking | ◐ | ◐ | ◐ | ◐ | ◐ | ✗ | ◐ | ✓ | ✓ | ✓ | ✗ | ✓ | ◐ | ◐ | ◐ |
| 29 | Recognition / kudos | ◐ | ✗ | ✓ | ✗ | ◐ | ✗ | ◐ | ✓ | ✓ | ✗ | ✗ | ◐ | ✗ | ✗ | ✗ |
| 30 | Surveys / engagement (eNPS) | ◐ | ◐ | ✓ | ◐ | ✓ | ✗ | ◐ | ✓ | ◐ | ✗ | ✗ | ◐ | ✗ | ◐ | ◐ |
| 31 | Approvals / workflow engine (configurable) | ◐ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ◐ | ◐ | ◐ | ✗ | ✓ | ◐ | ✓ | ✓ |
| 32 | Reporting and dashboards | ◐ | ✓ | ◐ | ✓ | ◐ | ◐ | ✓ | ✓ | ◐ | ◐ | ◐ | ◐ | ◐ | ✓ | ✓ |
| 33 | Audit trail / change history | ◐ | ✓ | ✓ | ✓ | ◐ | ◐ | ✓ | ✓ | ◐ | ◐ | ◐ | ✓ | ◐ | ✓ | ✓ |
| 34 | AI assistant (Q&A) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ | ◐ | ◐ | ✗ | ◐ |
| 35 | AI agent actions (human-in-the-loop) | ✗ | ✓ | ✓ | ◐ | ◐ | ◐ | ✓ | ✓ | ✓ | ✗ | ✗ | ◐ | ✗ | ✗ | ◐ |
| 36 | Open API / webhooks / marketplace | ◐ | ✓ | ✓ | ◐ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✓ |
| 37 | SSO / SCIM (Entra ID, Okta) | ◐ | ✓ | ✓ | ◐ | ✓ | ◐ | ✓ | ◐ | ◐ | ◐ | ✗ | ✓ | ◐ | ◐ | ◐ |
| 38 | Role-based access and field-level permissions | ✓ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ✓ | ◐ | ◐ | ◐ | ✓ | ◐ | ✓ | ✓ |
| 39 | WHS incident reporting | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ◐ | ✗ | ◐ | ✗ | ◐ | ✗ | ✗ | ✗ |
| 40 | Right-to-disconnect controls (quiet hours / out-of-hours notification rules) | ✗ | ✗ | ◐ | ✗ | ✗ | ✗ | ✗ | ◐ | ◐ | ✗ | ✗ | ◐ | ✗ | ✗ | ✗ |
| 41 | Record retention (7-year) and export for inspectors | ◐ | ◐ | ◐ | ◐ | ◐ | ◐ | ◐ | ✓ | ◐ | ✓ | ✓ | ✓ | ✗ | ◐ | ◐ |
| 42 | Assets / equipment tracking | ◐ | ✓ | ◐ | ◐ | ✓ | ✗ | ✓ | ◐ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ |
| 43 | Expenses | ✗ | ✓ | ✗ | ✗ | ◐ | ✗ | ◐ | ✓ | ✗ | ✗ | ◐ | ✗ | ◐ | ✓ | ✓ |
| 44 | Recruitment / ATS | ◐ | ✓ | ✓ | ◐ | ◐ | ◐ | ◐ | ✓ | ✗ | ✗ | ✗ | ✓ | ◐ | ✓ | ✓ |
| 45 | Compensation planning / benefits | ◐ | ✓ | ✓ | ◐ | ◐ | ✓ | ◐ | ◐ | ✗ | ✗ | ✗ | ◐ | ✗ | ◐ | ◐ |
| 46 | Employee directory / announcements / chat | ✓ | ◐ | ✓ | ◐ | ✓ | ◐ | ◐ | ✓ | ✓ | ◐ | ✗ | ◐ | ◐ | ◐ | ✓ |
| 47 | Multi-entity / multi-location | ◐ | ✓ | ✓ | ◐ | ✓ | ◐ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | ◐ | ✓ | ✓ |

**Notes on the matrix**
- Rows 7, 12, 16, 20, 22, 23, 39, 40 and 41 are **Australian-specific**. Here the AU vendors (Employment Hero, Tanda, Humanforce, and Deputy for time) dominate, and global and open-source products are weak.
- Rows 39 (WHS incidents) and 40 (right to disconnect) are **thinly served by everyone**. Most products leave them to policy documents or to dedicated WHS tools.
- "◐" for global products on AU leave usually means configurable leave policies **without** NES-correct accrual logic (for example, accrual on ordinary hours or the paid FDV leave payslip rule).

---

## 6. Australian-specific must-haves (product requirements view)

| Obligation | What the rule says (summary) | Product implication for EMS |
|---|---|---|
| **National Employment Standards (NES)** | 38-hour maximum week plus reasonable additional hours. Flexible work requests. 12 months unpaid parental leave (plus a request for 12 more). 4 weeks annual leave (+1 for some shiftworkers). 10 days paid personal/carer's leave (pro rata for part-time) plus 2 days unpaid carer's. 2 days compassionate. **10 days paid family and domestic violence leave** (casuals included). Community service leave (jury pay up to 10 days). Long service leave (state-based). Public holidays. Notice up to 5 weeks and redundancy up to 16 weeks. Super contributions. Casual conversion pathways. | Seeded NES leave types, accrual on ordinary hours, pro rata for part-time, casual entitlement rules, state-based public holidays and LSL as configurable rules. |
| **Paid FDV leave confidentiality** | Payslips **must not** mention paid family and domestic violence leave. It must be recorded, but shown as ordinary hours or another payment. | A leave type with a "confidential" flag that is hidden from payslip exports and restricted in reports and audit views. |
| **Modern awards and penalty rates** | Awards set minimum rates, penalties, overtime and allowances. AU vendors maintain award templates themselves; none is Fair Work certified. | Don't build a full award engine. Model award classification, base rate and simple penalty rules, and integrate with payroll (Employment Hero/KeyPay, Xero) for interpretation. Show transparent calculations. |
| **Single Touch Payroll (STP Phase 2)** | Payroll events are reported to the ATO each pay run. | **Integrate, don't build.** Export approved timesheets and leave to Xero Payroll, Employment Hero or MYOB, which lodge STP. |
| **Superannuation – Payday Super (from 1 July 2026)** | Super guarantee must be paid **with each wage payment** and reach the fund within **7 business days**. A new employee's first contribution has 20 business days. Previously super was due quarterly. | Store super fund choice and stapled-fund status during onboarding, and pass them to payroll. Flag missing fund details before the first pay run. |
| **Fair Work Information Statement (FWIS) / Casual Employment Information Statement (CEIS)** | FWIS goes to every new employee before or as soon as possible after they start (email or link is fine). CEIS goes to casuals at the start and at set points during employment. | An onboarding task that issues and logs FWIS/CEIS (with date, version and acknowledgement), plus CEIS reminders for casuals. |
| **Right to disconnect** | Applies to non-small businesses from 26 Aug 2024 and **small businesses from 26 Aug 2025**. Employees may refuse unreasonable out-of-hours contact. "Contact" includes calls, email, texts and messaging apps. | Per-employee working hours and "quiet hours". Hold non-urgent notifications until working hours. Mark on-call and emergency exceptions. Record contact expectations in the employee profile. |
| **Record keeping (7 years)** | Keep employment basis, pay rates, hours (casual and irregular part-time, overtime), leave balances and agreements, IFAs, termination details and super contributions **for 7 years**. Records must be legible, in English, unaltered except to correct errors, and available to inspectors. Payslips must be issued within **1 working day** of pay day. If records are missing, courts presume the employee's wage claim is correct. | Immutable audit trail (who/what/when, correction notes), soft delete only, a 7-year retention policy, an "inspector export" pack, and agreement records (leave in advance, cash-out, time off instead of overtime). |
| **Wage theft criminal offence (from 1 Jan 2025)** | Intentional underpayment is now a criminal offence. | Strengthens the case for transparent timesheets, approvals and audit logs, a key selling point. |
| **WHS incident reporting** | Notifiable incidents (death, serious injury or illness, dangerous incidents, and violent incidents with serious psychological risk) must be reported to the regulator **immediately**. Records must be kept for **at least 5 years**, and the site preserved. Updated model laws (Dec 2025) add notification of **15+ consecutive days of work-related absence** (within 14 days) and work-related suicide or attempted suicide, where adopted by the state or territory. | An incident register with a "notifiable?" decision helper, regulator-notified fields (date, reference), attachments, a 5-year retention rule and a link to leave records for extended absences. |

---

## 7. Table stakes vs differentiators

### 7.1 Table stakes (users expect these; missing them is disqualifying)
- Employee records with role-based access, a directory and an org chart
- Employee self-service (profile, payslips via payroll integration, leave balances) on mobile
- Leave requests, approvals and balances, **with NES-correct leave types for AU**
- Onboarding and offboarding checklists, document storage, e-signature or acknowledgement
- Timesheets and clock in/out with manager approval
- Basic reporting (headcount, leave liability, attendance, turnover) with CSV/Excel export
- Audit trail and permissions
- Integration with at least one AU payroll system (Xero Payroll, Employment Hero/KeyPay, MYOB)
- Notifications via email and push
- **An AI assistant for HR Q&A.** By 2026 this is table stakes in nearly every suite (BambooHR, HiBob, Personio, Zoho, Deel, Employment Hero, Deputy).

### 7.2 Differentiators (where products win)
- **Award interpretation and rostering intelligence:** Deputy, Tanda, Humanforce, Employment Hero
- **Agentic AI with human approval** for leave, rostering, payroll pre-checks and report building: Employment Hero, Deputy, Rippling, Workday
- **Transparent, explainable AI with audit logs:** HiBob's "glass box"
- **Unified HR + IT + spend** (provisioning from the employee record): Rippling
- **Culture and engagement depth:** HiBob
- **Published, simple pricing with month-to-month terms:** Gusto, Tanda bundle, Zoho
- **Global hiring/EOR:** Deel, Rippling
- **Compliance-by-design for AU obligations that are thinly served** (right to disconnect, WHS incidents, FWIS/CEIS evidence, 7-year inspector-ready records): **nobody owns this outright, so it is an open differentiator**

---

## 8. Market gaps and recurring pain points a small, focused product could exploit

| # | Recurring pain point (evidence) | Gap / opportunity for EMS |
|---|---|---|
| 1 | **Minimums and add-on creep** (Employment Hero 10-user minimum; BambooHR flat fee for 25 or fewer staff; rostering, chat and payroll sold as add-ons) | One clear "everything core" tier with no minimum seat count, and pricing logic that is explainable. As a portfolio project this means demonstrating a clean **plan/feature-flag model**. |
| 2 | **Opaque pricing and renewals** (HiBob, Rippling, Personio, Humanforce quote-only; about 20% renewal increases reported for HiBob) | Publish the model. Use feature flags and capability-based access (EMS already has role capability access) rather than hidden modules. |
| 3 | **Award misconfiguration leads to underpayment, and the vendor caps its liability** (Deputy review; Employment Hero setup complexity) | **Explainable calculations.** Show *why* a shift attracted a penalty or why a leave balance is what it is (rule, inputs, result), with an audit trail. Don't try to out-build award engines; make results verifiable. |
| 4 | **Hard to reach human support** (Employment Hero's top complaint) | Out of scope for a portfolio project, but good in-app guidance, validation and "why can't I do this?" messages reduce support load. AI help grounded in the product's own docs. |
| 5 | **Rigid or limited reporting** (Humanforce, OrangeHRM, HiBob cross-module, BambooHR insight gaps) | A strong reporting layer: saved filters, scheduled exports, leave liability, attendance exceptions and absenteeism (EMS already has attendance summary, punctuality and absenteeism endpoints), plus a **natural-language report builder** like Employment Hero's 2026 feature. |
| 6 | **Mobile/desktop inconsistency and notification overload** (Deputy) | One responsive Next.js UI (or PWA) with a **notification policy engine** that also delivers right-to-disconnect compliance (quiet hours, urgency levels, digests). |
| 7 | **Right to disconnect and WHS incidents are poorly served** by HR suites | Ship both as first-class modules. They are low build cost, high compliance value, rarely done well, and easy to demo. |
| 8 | **FWIS/CEIS and 7-year records are handled as "documents" without evidence** | Onboarding compliance pack: issue, track and acknowledge FWIS/CEIS, collect super fund choice and TFN declaration status, and produce a 7-year retention and inspector export. |
| 9 | **Open source lacks AU compliance; global HRIS lacks AU payroll** | An AU-first core HR on .NET with **clean integration adapters** (Xero Payroll first, then Employment Hero/KeyPay) shows business correctness and integration skill. |
| 10 | **AI is opaque and increasingly metered** (Personio AI Credits from Aug 2026, Workday Flex Credits) | **Bounded, explainable AI:** a policy Q&A assistant grounded in company documents, "draft-then-approve" actions (leave approval suggestions, roster gap fill, anomaly flags on timesheets), every AI action logged, and no automated people evaluation (matching HiBob's and Deputy's stance). |
| 11 | **Small businesses (5–50 staff) are over-served by suites and under-served by Xero alone** | Target the "Xero Payroll + spreadsheets" business. Provide the HR layer around Xero: records, leave, onboarding compliance, timesheets, incidents and documents. |

---

## 9. Pricing landscape summary

| Segment | Products | Model | Indicative price (per employee/user per month) | Source date |
|---|---|---|---|---|
| Ultra-low-cost suite | Zoho People | Per user, published, free tier | USD ~1.25–8 (sources conflict) | Oct 2026 |
| AU rostering / time | Deputy | Per user, published, $30 min | AUD 6.75 / 8.75 / 13 (ex GST) | Mar–Sep 2026 |
| AU WFM + HR + payroll bundle | Tanda / Workforce.com | Per user, bundle published | AUD 12.80 (ex GST) | Sep 2026 |
| AU HR suite | Employment Hero | Per employee, 10-user min, add-ons | AUD 11 (Essentials) / 15 (Engage); +5 rostering; realistic 15–30+ for small award teams | Jul–Nov 2026 |
| AU payroll-first | Xero Payroll | Included in accounting plan | ~AUD 42/month plan (Growing), not per employee | May 2026 |
| AU frontline enterprise | Humanforce | Quote | Not published | Oct 2026 |
| Global SMB HRIS | BambooHR | Per employee or flat ≤25 | USD 10 / 17 / 25; flat 250 / 425 / 650 per month | Oct 2026 |
| Global HR + IT + spend | Rippling | Base + per employee, modular, quote | ~USD 35–40 base + ~8 per employee | Jun–Oct 2026 |
| Mid-market HRIS | HiBob | Quote, modular | ~USD 5–10 Core; 16–25 with modules; implementation 10–20% | 2026 |
| EU HRIS | Personio | Quote + **metered AI credits** | Last public €2.88–4.43; AI €0.01/credit | Aug–Oct 2026 |
| US payroll SMB | Gusto | Base + per person, published, monthly | USD 49+6 / 80+12 / 180+22 | Nov 2026 |
| Global workforce | Deel | Per employee; EOR separate | USD 5 (Core) / 19 (Advanced); EOR 599 | Oct–Nov 2026 |
| Enterprise | Workday | Quote + Flex Credits for AI | Not published | Sep 2025 / 2026 |
| Open source | OrangeHRM, Frappe HR, Odoo | Free self-host; paid cloud/enterprise | Frappe Cloud from ~USD 16/site/month; Odoo AU ~AUD 34–80 per user | 2026 |

**Pricing patterns to note**
1. **Per-employee-per-month is universal.** Bands are roughly AUD 5–13 for time and rostering and AUD/USD 10–25 for HR suites.
2. **Minimums and flat floors** make the effective per-head cost two to three times the headline rate for teams under 10.
3. **Quote-only pricing rises with company size and complexity.** Transparency is itself a differentiator (Gusto, Tanda, Zoho).
4. **AI is becoming a separately metered line item** (Personio, Workday), while AU vendors (Employment Hero, Deputy) currently bundle AI into existing plans.

---

## 10. Implications for EMS (for the BA to take forward)

The CEO and BA should validate these against the current EMS codebase and roadmap.

1. **Positioning:** An "AU small-business core HR + time, compliance-first and explainable" portfolio product. Payroll/STP/super stays as **integrations** (Xero Payroll first).
2. **Close table-stakes gaps first:** NES leave types and accrual, onboarding with FWIS/CEIS evidence, documents with acknowledgement, timesheet approval, a reporting/export layer, a mobile-friendly UI and an audit trail.
3. **Pick two or three differentiators that are cheap to build and easy to demo:** right-to-disconnect notification policy, a WHS incident register, a 7-year retention and inspector export, and an explainable leave/time calculation view.
4. **AI scope:** A grounded policy Q&A assistant and "draft-then-approve" agent actions with full logging. Avoid AI-driven performance judgements.
5. **Do not attempt:** A full award interpretation engine, STP lodgement, global payroll/EOR, or IT device management.

---

## Sources

Accessed 15 November 2026 unless noted. Vendor-competitor authorship is flagged where relevant.

**Global HRIS**
- BambooHR pricing guide (Compono, competitor; reviewed Oct 2026): https://www.compono.com/articles/bamboohr-pricing-guide-2026
- Best HR software of 2026 (pricing and AI by vendor; checked 26 Oct 2026): https://hackernoon.com/best-hr-software-of-2026
- Rippling pricing guide (People Managing People, Oct 2026): https://peoplemanagingpeople.com/tools/rippling-pricing/
- Rippling pricing (Gusto, competitor; June 2026): https://gusto.com/resources/guides/switch-payroll-providers/rippling-pricing
- HiBob pricing page: https://www.hibob.com/pricing-plans/
- HiBob reviews deep dive (Skima AI, integration vendor): https://skima.ai/blog/product-deep-dives/hibob-reviews
- Personio pricing blueprint and 2026 change log (AI Credits, Assistant): https://www.usagepricing.com/blueprint/personio
- Workday Illuminate agents and Flex Credits press release (16 Sep 2025): https://newsroom.workday.com/2025-09-16-Workday-Illuminate-TM-Expands-with-New-AI-Agents-for-HR,-Finance,-and-Industry
- Zoho People pricing 2026 (Noon, competitor; 26 Oct 2026): https://www.noon.ai/blog/articles/437-zoho-people-pricing-2026
- Zoho People on Capterra: https://www.capterra.com/p/110931/Zoho-People/pricing/
- Deel review (The CTO Club, Oct 2026): https://thectoclub.com/tools/deel-review/

**Australian market**
- Employment Hero AU pricing: https://employmenthero.com/au/pricing/
- Employment Hero 2026 product recap: https://employmenthero.com/blog/employment-hero-product-updates-2026/
- Employment Hero review (Scale Suite, EH partner; July 2026): https://www.scalesuite.com.au/resources/employment-hero-review-2026
- Employment Hero reviews roundup (RosterElf, competitor; Nov 2026): https://rosterelf.com/reviews/employment-hero
- KeyPay becomes Employment Hero (2023): https://employmenthero.com/sg/blog/keypay-becomes-employment-hero/
- Deputy vs Tanda (SoftwareHQ, verified Sep 2026): https://softwarehq.pages.dev/compare/deputy-vs-tanda
- Workforce.com (Tanda) review (SoftwareHQ, Sep 2026): https://softwarehq.pages.dev/review/workforce-com
- Deputy reviews (Capterra AU): https://www.capterra.com.au/reviews/147589/deputy
- Deputy AI: https://www.deputy.com/ai
- AU payroll comparison incl. Xero Payroll (TradieScaler, May 2026): https://tradiescaler.pages.dev/hr-payroll
- Humanforce review (People Managing People, Oct 2026): https://peoplemanagingpeople.com/tools/humanforce-review/
- Humanforce reviews (Capterra AU): https://www.capterra.com.au/reviews/122077/humanforce

**Open source**
- Open-source HR alternatives 2026 (IceHrm, competitor): https://icehrm.com/blog/the-7-best-open-source-hr-software-alternatives-in-2026/
- OrangeHRM reviews (Capterra UK): https://www.capterra.co.uk/reviews/49068/orangehrm
- Frappe HR documentation: https://docs.frappe.io/hr/introduction.md
- Odoo pricing Australia (OEC.sh, Oct 2026): https://oec.sh/odoo-pricing/australia

**Australian compliance**
- Fair Work – Introduction to the National Employment Standards: https://www.fairwork.gov.au/tools-and-resources/fact-sheets/minimum-workplace-entitlements/introduction-to-the-national-employment-standards
- Fair Work – Fair Work Information Statement: https://www.fairwork.gov.au/employee-entitlements/national-employment-standards/fair-work-information-statement
- Fair Work – Casual Employment Information Statement: https://www.fairwork.gov.au/employee-entitlements/national-employment-standards/casual-employment-information-statement
- Fair Work – Record-keeping and pay slips fact sheet: https://www.fairwork.gov.au/tools-and-resources/fact-sheets/rights-and-obligations/record-keeping-pay-slips
- Fair Work – Right to disconnect: https://www.fairwork.gov.au/right-to-disconnect
- Fair Work – Right to disconnect for small business from 26 August 2025: https://www.fairwork.gov.au/newsroom/news/right-to-disconnect-for-small-business-employees-starts-26-august
- Fair Work – Payday Super from 1 July 2026: https://www.fairwork.gov.au/newsroom/news/payday-super-new-rules-starting-1-july-2026
- DEWR – Wage theft criminal offence from 1 January 2025: https://www.dewr.gov.au/closing-loopholes/announcements/new-wage-theft-criminal-offence-will-commence-1-january-2025
- Safe Work Australia – WHS notifications fact sheet (Dec 2025 update): https://www.safeworkaustralia.gov.au/sites/default/files/2026-03/whs_notifications_incidents_extended_absences_suicides_factsheet_dec2025_update.pdf
