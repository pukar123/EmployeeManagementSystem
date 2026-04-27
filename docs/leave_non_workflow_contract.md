# Leave API Contract (Workflow Deferred)

This document defines the current leave-management contract while approval workflow is deferred.

## Lifecycle Scope

- Supported request lifecycle: `Pending`, `ModifiedPending`, `Cancelled`.
- Approval states exist in enum for forward compatibility but are not used in current flow.
- All leave controllers return error payloads in the shape:
  - `{ "message": "..." }`

## Endpoints

### Leave Types

- `GET /api/LeaveTypes?organizationId={id}`
- `GET /api/LeaveTypes/{id}`
- `POST /api/LeaveTypes`
- `PUT /api/LeaveTypes/{id}`

### Leave Balances

- `GET /api/LeaveBalances/employee/{employeeId}`
- `GET /api/LeaveBalances/employee/{employeeId}/type/{leaveTypeId}`

### Leave Requests

- `GET /api/LeaveRequests/employee/{employeeId}`
- `GET /api/LeaveRequests/{id}`
- `POST /api/LeaveRequests`
- `PUT /api/LeaveRequests/{id}`
- `POST /api/LeaveRequests/{id}/cancel`

## Error Semantics

- `404 Not Found`: target entity does not exist.
- `400 Bad Request`: business rule or validation failure.

## Future Workflow Integration

- Approval endpoints (`approve`, `reject`) and routing logic are intentionally excluded.
- Existing request statuses and payloads should remain backward compatible when workflow module is added.
