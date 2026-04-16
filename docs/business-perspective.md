# EMS Business Perspective

This document explains the Employee Management System (EMS) from a business point of view: why it exists, who uses it, what outcomes it enables, current scope, and next priorities.

## 1) Business purpose

EMS centralizes core workforce operations so HR and operations teams can maintain accurate employee records, organizational structure, and access control from one system.

### Problems EMS addresses

- Employee data spread across files or disconnected tools
- Inconsistent role-based access to operational modules
- Slow onboarding/offboarding updates
- Limited visibility into attendance and organizational setup readiness

### Intended business outcomes

- Faster employee lifecycle operations (joiner/mover/leaver basics)
- Better data consistency across departments and sites
- Reduced unauthorized feature exposure through role-permission controls
- Improved operational readiness with auditable, authenticated usage

## 2) Primary personas and responsibilities

### System Admin

- Controls environment setup and platform-level access
- Oversees role assignments and permission mappings
- Monitors authorization health and cutover readiness

### HR / Operations

- Maintains employee master records
- Manages departments, job positions, sites, and organization profile
- Uses attendance-related modules for daily workforce operations

### Manager (current/near-term)

- Consumes team-related employee and attendance data based on assigned role access

### Employee (future-facing)

- Potential self-service consumer for profile/attendance views as scope evolves

## 3) Current business capability map

### A. Access and security

- Authentication-based access to protected APIs/pages
- Role-key-based menu authorization (`RoleKeyPermissions`) controls visible/usable modules
- Admin role seeded with broad access defaults

### B. Organization setup

- Organization setup flow controls operational start state
- Core organization profile and branding data supported

### C. Workforce master data

- Employee management (create/read/update/delete)
- Department, location, job position, and site relationship support
- Employee number generation per organization

### D. Attendance operations

- Attendance endpoints and related workflow support for operational tracking

## 4) Hiring scope: what exists today vs. what is not yet implemented

### Available now (hiring foundation)

- Employee creation supports key onboarding fields:
  - organization, department, location, manager, job position
  - date joined and employment status
- Validation enforces organizational consistency for job position
- New employee code is generated automatically (`EMP###`)

### Not yet in scope (full recruiting/hiring lifecycle)

- Job requisition and approval workflow
- Candidate pipeline and interview stages
- Offer management and acceptance tracking
- Preboarding/onboarding checklist workflow automation

## 5) Key business rules (plain language)

- Protected modules require a valid authenticated user
- Menus and feature access are determined by role keys
- Organization setup must complete before normal operations
- Admin is expected to have broad baseline access
- Workforce data relationships must remain organization-consistent

## 6) Business KPIs (recommended)

- Time to create and activate a new employee record
- Data completeness rate for employee profiles
- Role/permission support incidents per month
- Attendance entry completeness and timeliness
- Time from environment setup to operational readiness

## 7) Risks and controls

### Risk: role-permission mismatch can block valid users

- Control: role-key normalization, seeded admin permissions, authorization diagnostics

### Risk: inconsistent master data entry

- Control: centralized CRUD services with validation in application layer

### Risk: scope growth without clear business prioritization

- Control: maintain capability map and staged roadmap in this document

## 8) Near-term business roadmap (proposed)

1. Stabilize role-permission experience and admin operability
2. Expand attendance reporting and exception handling
3. Add manager-focused team views and actions
4. Introduce structured onboarding task workflow
5. Evaluate employee self-service capability rollout

## 9) Document ownership and usage

- Audience: product owners, operations leads, engineering leads
- Update when business workflows, roles, or scope boundaries change
- Pair this with technical architecture docs:
  - `docs/architecture.md`
  - `docs/ARCHITECTURE_AND_PATTERNS.md`
