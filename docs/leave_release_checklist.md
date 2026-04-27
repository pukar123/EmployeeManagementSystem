# Leave Management Release Checklist (Workflow Deferred)

Use this checklist before releasing the non-workflow leave module.

## Backend

- [ ] `dotnet build EmployeeManagementSystem.sln` passes.
- [ ] Leave migration is applied in target environment.
- [ ] Leave controllers are reachable and authenticated.
- [ ] Contract docs match deployed endpoint behavior.

## Functional

- [ ] Leave type CRUD works.
- [ ] Leave policy rule CRUD works.
- [ ] Leave request create/update/cancel works.
- [ ] Leave balance validation blocks insufficient requests.
- [ ] Leave request overlap validation blocks conflicting requests.
- [ ] Leave attachment upload/download/delete works.
- [ ] Manual accrual run endpoint executes successfully.
- [ ] Leave-year reset endpoint executes successfully.
- [ ] Bulk import returns row-level outcomes.

## Frontend

- [ ] Leave navigation item is visible for authorized users.
- [ ] `/leave` page shows balances and requests.
- [ ] Request submission and cancel actions work.
- [ ] `/leave/admin` bulk import flow works.
- [ ] Frontend uses `NEXT_PUBLIC_API_BASE_URL` and shared HTTP client.

## Deferred

- [ ] Approval workflow remains deferred (no approve/reject routing in this release).
