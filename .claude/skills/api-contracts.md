---
name: api-contracts
description: MyCondo API design standards — versioning, error contract, pagination, permissions per endpoint. Use when adding or changing any HTTP endpoint.
---

# API Contracts

## Current actual state

`MyCondo.Api/Endpoints/` has four files as of 2026-08-01 (Wave 1 Frontend Slice 2):
`AuthEndpoints.cs` (`/api/v1/auth/{register,login,refresh,logout,change-password,me}`),
`TenantEndpoints.cs` (`/api/v1/tenants/{by-slug/{slug},"",{id}/activate,{id}/suspend}`),
`RoleEndpoints.cs` (`/api/v1/roles/{"",{id},{id}/permissions,{id}/permissions/{permissionId},
{id}/assignments,{id}/assignments/{userId}}` + `/api/v1/permissions`), and
`UserEndpoints.cs` (`/api/v1/users/{"",{id}/disable}`). `DELETE` is used for the three
"undo" operations (deactivate a role, remove a granted permission, revoke an assignment) — all
soft/relational removals, not hard deletes of history. `DELETE .../assignments/{userId}` takes an
optional `?buildingId=` query param mirroring `AssignRoleToUserCommand`'s scope. Note the read/write
asymmetry this exposed: every mutation endpoint (`GrantPermissionToRole`, `AssignRoleToUser`, etc.) had
existed for slices before a matching *read* endpoint did — `GET /api/v1/roles/{id}/permissions` and
`GET /api/v1/roles/{id}/assignments` only landed in Frontend Slice 2, once a UI actually needed to know
current state rather than just fire-and-forget writes. When adding a new mutation, add its read-back
query in the same slice unless there's a specific reason not to. These are the
reference pattern — copy their shape (route group per feature,
`MapXEndpoints` extension method, `.AllowAnonymous()`/`.RequireAuthorization()`/`.RequirePermission(...)`
on every route, `ISender` injected per-endpoint, `.Produces<T>()`/`.Produces(status)` declared on every
route so OpenAPI codegen emits real types instead of `unknown` — found the hard way in Frontend Slice 1)
for new endpoints rather than improvising. No other business-module endpoints exist yet — those land
with their respective waves.

The permission catalogue is seeded (47 concrete permissions, `Seed_Permission_Catalogue` migration —
see `mycondo-docs/07-delivery/MASTER_BACKLOG.md` ID-2) and `RequirePermission` checks are therefore
reachable in practice, not just correctly-shaped: `RegisterUserCommandHandler` grants the first user of
each tenant a `SuperAdmin` role holding every catalogue permission, so a fresh tenant's first
registrant can immediately call any permission-gated endpoint, including `POST /api/v1/roles` to grant
narrower roles to everyone after them.

## Style

Resource-oriented REST with explicit action endpoints for workflows, versioned under `/api/v1/...`:

```http
GET    /api/v1/properties
POST   /api/v1/properties
POST   /api/v1/billing-runs/{id}/execute
POST   /api/v1/invoices/{id}/void
POST   /api/v1/payments/{id}/reverse
```

## Every endpoint must

- Declare `.RequirePermission("<module>.<resource>.<action>")` (see
  `MyCondo.Api/Authorization/EndpointRequirePermissionExtensions.cs` — composes `RequireAuthorization()`
  + a `PermissionEndpointFilter`) or explicitly `.AllowAnonymous()`/`.RequireAuthorization()` (plain
  authentication, no specific permission — e.g. acting on your own account) — never leave it implicit.
  Permission checks currently read JWT `perm` claims, not a server-side/Redis lookup (ADR-011) —
  that's deliberate, not a shortcut to fix.
- Bind via a request DTO, dispatch via `Mediator`, return a response DTO — no `DbContext` access, no
  business logic in the endpoint delegate/controller action itself.
- Return Problem Details on error, with a machine-readable `code` field (see the error contract
  example below) and a correlation ID (the `CorrelationIdMiddleware` already sets this).

## Error contract

```json
{
  "type": "https://mycondo/errors/lease-overlap",
  "title": "Lease period overlaps an existing lease.",
  "status": 409,
  "code": "LEASE_PERIOD_OVERLAP",
  "detail": "The selected unit already has an active lease during this period.",
  "correlationId": "01J...",
  "errors": { "startDate": ["The lease overlaps lease LS-2026-0042."] }
}
```

`GlobalExceptionMiddleware` and `AddProblemDetails()` are already wired — extend the exception→status
mapping there rather than duplicating error shaping per endpoint.

## Other requirements

Consistent pagination/sorting/filtering conventions (not yet established — first list endpoint sets
the pattern), optimistic concurrency (ETag or a version column), UTC timestamps everywhere, no
stack-trace exposure in any environment, OpenAPI-generated frontend client (already the plan via
`openapi-typescript` + `@rtk-query/codegen-openapi` on the frontend side — don't hand-write a
duplicate frontend type for anything the backend already exposes via OpenAPI).
