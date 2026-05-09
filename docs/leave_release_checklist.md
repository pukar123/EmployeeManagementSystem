# Leave Management Release Checklist (Workflow Deferred)

Use this checklist before releasing the non-workflow leave module.

## Backend

- [x] `dotnet build EmployeeManagementSystem.sln` passes.
- [x] Leave migration is applied in target environment.
- [x] Leave controllers are reachable and authenticated.
- [x] Contract docs match deployed endpoint behavior.

## Functional

- [x] Leave type CRUD works.
- [x] Leave policy rule CRUD works.
- [x] Leave request create/update/cancel works.
- [x] Leave balance validation blocks insufficient requests.
- [x] Leave request overlap validation blocks conflicting requests.
- [x] Leave attachment upload/download/delete works.
- [x] Manual accrual run endpoint executes successfully.
- [x] Leave-year reset endpoint executes successfully.
- [x] Bulk import returns row-level outcomes.

## Frontend

- [x] Leave navigation item is visible for authorized users.
- [x] `/leave` page shows balances and requests.
- [x] Request submission and cancel actions work.
- [x] `/leave/admin` bulk import flow works.
- [x] Frontend uses `NEXT_PUBLIC_API_BASE_URL` and shared HTTP client.

## Deferred

- [x] Approval workflow remains deferred (no approve/reject routing in this release).
