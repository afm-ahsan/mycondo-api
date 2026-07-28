---
name: api-contracts
description: MyCondo API design standards — versioning, error contract, pagination, permissions per endpoint. Use when adding or changing any HTTP endpoint.
---

# API Contracts

## Current actual state

`MyCondo.Api` exposes only health checks and the OpenAPI/Scalar UI today — **no business endpoints
exist yet**, including for the fully-implemented Auth commands (Login/Register/RefreshToken/Logout).
There is no `Endpoints/` or `Controllers/` folder and no `/api/v1` version prefix in use anywhere yet.
When you add the first real endpoint, you are establishing the pattern other endpoints will copy —
follow the rules below exactly rather than improvising.

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

- Declare `[RequirePermission("<module>.<resource>.<action>")]` or explicitly `[AllowAnonymous]` —
  never leave it implicit. (The `[RequirePermission]` mechanism doesn't exist yet — building it is
  part of the same Wave 1 work that wires the first endpoints; don't ship an endpoint without it.)
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
