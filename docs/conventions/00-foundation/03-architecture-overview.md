# Architecture Overview

This document gives the high-level shape of every project we build. Detail rules live in the specific files (`01-backend/`, `02-frontend/`, etc.). Use this as the mental model.

---

## System Topology

```
                        ┌─────────────────────────────┐
                        │         Browser             │
                        │  React + Vite + Metronic    │
                        │  RTK Query / RHF + Zod      │
                        └──────────────┬──────────────┘
                                       │ HTTPS / JSON
                                       │ Bearer JWT
                                       ▼
                        ┌─────────────────────────────┐
                        │   Reverse Proxy (nginx)     │
                        │   TLS termination, gzip     │
                        └──────────────┬──────────────┘
                                       │
              ┌────────────────────────┼────────────────────────┐
              ▼                        ▼                        ▼
    ┌──────────────────┐   ┌────────────────────┐   ┌────────────────────┐
    │  ASP.NET Core    │   │   Background       │   │   Static Web       │
    │  Minimal API     │   │   Workers          │   │   (built React)    │
    │  (Project.Api)   │   │  (Project.Workers) │   │   served by nginx  │
    └────┬─────────────┘   └────────┬───────────┘   └────────────────────┘
         │                          │
         ▼                          ▼
    ┌────────────────────────────────────┐
    │ Application + Domain layers        │
    │ MediatR handlers, validators       │
    │ Aggregates, value objects, events  │
    └────┬───────────────────────────────┘
         ▼
    ┌────────────────────────────────────┐
    │ Infrastructure                     │
    │ EF Core 10, Dapper, Redis, RabbitMQ│
    └────┬─────────────────────┬─────────┘
         │                     │
         ▼                     ▼
   ┌────────────┐         ┌─────────┐
   │ PostgreSQL │         │  Redis  │
   └────────────┘         └─────────┘
```

---

## Backend Architecture: Clean Architecture + CQRS

Every backend service ships with five core projects:

```
src/
├── <Project>.Domain          ← entities, value objects, events, abstractions
├── <Project>.Application     ← CQRS handlers, validators, behaviors, DTOs
├── <Project>.Infrastructure  ← EF Core, repositories, Redis, external clients
├── <Project>.Api             ← endpoints, middleware, DI composition
└── <Project>.Shared          ← truly cross-cutting primitives (Result, guards)
```

Plus tests:

```
tests/
├── <Project>.Domain.UnitTests
├── <Project>.Application.UnitTests
├── <Project>.Infrastructure.IntegrationTests
└── <Project>.Api.IntegrationTests
```

### Dependency direction

```
Api ──► Application ──► Domain
 │           ▲
 └─► Infrastructure ─┘
```

- `Domain` references **nothing**.
- `Application` references **only** `Domain`.
- `Infrastructure` references `Domain` + `Application`.
- `Api` references all three.

Enforced in `.csproj` and validated by `NetArchTest` in CI.

### Request flow (write path)

```
HTTP POST /api/<aggregate>s
    │
    ▼
Endpoint (Api)
    │  parses request → builds command
    ▼
ValidationBehavior (MediatR pipeline)
    │  FluentValidation runs; throws on invalid
    ▼
LoggingBehavior + PerformanceBehavior
    │  structured log, time-tracking
    ▼
CommandHandler (Application)
    │  loads aggregate, calls domain method
    ▼
Aggregate (Domain)
    │  enforces invariants, raises events
    ▼
Repository.Save + UnitOfWork.SaveChangesAsync
    │  EF Core writes; DispatchDomainEventsInterceptor fires events
    ▼
EventHandlers (Application) react to domain events
    │
    ▼
HTTP 201 Created
```

### Request flow (read path)

```
HTTP GET /api/<aggregate>s?filter=...
    │
    ▼
Endpoint (Api)
    │  parses query → builds query DTO
    ▼
QueryHandler (Application)
    │  calls IReadRepository
    ▼
ReadRepository (Infrastructure)
    │  Dapper SQL or EF AsNoTracking projection
    ▼
DTOs returned, never entities
    │
    ▼
HTTP 200 OK
```

### Cross-cutting concerns

| Concern              | Where it lives                                                                      |
|----------------------|-------------------------------------------------------------------------------------|
| Validation           | `Application/Common/Behaviors/ValidationBehavior.cs` (MediatR pipeline)             |
| Logging              | `Application/Common/Behaviors/LoggingBehavior.cs` + Serilog                         |
| Performance          | `Application/Common/Behaviors/PerformanceBehavior.cs` (warns on > N ms)             |
| Authorization        | `[Authorize]` on endpoint groups; policy/role checks                                |
| Auditing             | `Infrastructure/Persistence/Interceptors/AuditInterceptor.cs`                       |
| Domain events        | `Infrastructure/Persistence/Interceptors/DispatchDomainEventsInterceptor.cs`        |
| Outbox               | `Infrastructure/Messaging/Outbox/OutboxProcessor.cs` (BackgroundService)            |
| Global error handler | `Api/Middleware/GlobalExceptionMiddleware.cs` → RFC 9457 ProblemDetails             |
| Correlation ID       | `Api/Middleware/CorrelationIdMiddleware.cs`                                         |
| Idempotency          | `Api/Middleware/IdempotencyMiddleware.cs` (Redis-backed)                            |

---

## Frontend Architecture: Feature-Sliced + Metronic

```
src/
├── main.tsx
├── App.tsx
├── auth/                          ← (Metronic auth scaffolding, kept)
├── components/
│   ├── ui/                        ← shadcn/Metronic primitives
│   ├── layout/                    ← AppShell, Sidebar, Topbar
│   └── feedback/                  ← ErrorBoundary, EmptyState, LoadingSpinner
├── modules/                       ← business features (the heart of the app)
│   └── <feature>/
│       ├── api/                   ← RTK Query slice
│       ├── components/            ← feature-private UI
│       ├── hooks/                 ← feature-private hooks
│       ├── pages/                 ← list, detail, create, edit
│       ├── schemas/               ← Zod schemas
│       └── index.ts               ← public surface
├── routes/                        ← route configuration / lazy
├── store/                         ← Redux store + middleware setup
├── api/                           ← shared baseQuery, interceptors
├── lib/                           ← pure utilities, no React
├── styles/                        ← Tailwind globals + Metronic CSS
└── types/                         ← cross-cutting TypeScript types
```

### Data flow

```
User action
    │
    ▼
React component (form / button)
    │
    ▼
RTK Query mutation hook  ◄── React Hook Form + Zod schema
    │
    ▼
baseQuery (interceptors: auth, idempotency-key, correlation-id)
    │
    ▼
HTTPS request → backend
    │
    ◄── ProblemDetails JSON on error
    │
    ▼
RTK Query cache update / invalidation
    │
    ▼
Components re-render with fresh server data
```

### Module isolation rules

- A module's **public surface** is `modules/<feature>/index.ts`. Other modules import only from there.
- A module's **internals** (components, hooks, schemas) are private. ESLint `import/no-restricted-paths` enforces it.
- Server data **never leaves RTK Query**. Components consume it via hooks; do not copy it into Redux slices or `useState`.

---

## Authentication Flow

```
1. POST /api/auth/login (email, password)
       │
       ▼
2. Backend validates → issues access token (short TTL ~15m) + refresh token (long TTL ~7d)
       │
       ▼
3. Frontend stores access token in memory; refresh in HTTP-only cookie
       │
       ▼
4. Every request: baseQuery attaches Authorization: Bearer <access>
       │
       ▼
5. On 401: silent refresh via refresh cookie → retry original request
       │
       ▼
6. On refresh failure: clear store, redirect to /login
```

Detail rules: `05-security/01-authentication-and-authorization.md`.

---

## Module Implementation Order

When bootstrapping a new project, build in this order. Skipping ahead causes rework.

```
1. Foundation
   └── Solution skeleton, Directory.Build.props, Directory.Packages.props,
       global.json, .editorconfig, docker-compose, CI pipeline

2. Cross-cutting
   └── DbContext, audit, soft-delete, domain-event dispatch, logging,
       OpenTelemetry, error middleware, idempotency middleware

3. Identity & RBAC
   └── User, Role, Permission, JWT auth, refresh flow, login/register endpoints,
       authorization policies, frontend login page + protected routes

4. Master data
   └── Lookups, configuration, reference tables (per project — e.g. Country,
       Currency, ProductCategory)

5. Core business modules
   └── One vertical slice at a time:
       Domain entity → Application handlers → Infrastructure config →
       API endpoints → Frontend list page → Create/edit form → Detail page
       (Includes unit + integration + E2E tests for the slice)

6. Reports & dashboards
   └── Read-only projections; cached in Redis; rendered with charts on FE

7. Polish
   └── Pagination, sorting, filtering across lists; empty states; loading skeletons;
       a11y audit; performance budgets

8. Pre-prod
   └── Security audit, secret review, backup/restore drill, observability dashboards,
       runbook, deployment dry-run
```

---

## Modular Monolith vs Microservices

Default: **modular monolith**. One solution, multiple modules sharing a deploy unit and one PostgreSQL instance.

Split into separate services only when:
- A module has dramatically different scaling needs.
- A module has dramatically different release cadence.
- A module needs a different language/runtime.
- Team size justifies the operational cost (typically 8+ engineers per service).

If splitting:
- Use the modular layout at `src/Modules/<Context>/...` (see `01-backend/01-solution-structure.md` §9).
- Modules talk via integration events on a message bus, never direct in-process calls.
- Each module owns its schema; no shared tables.

---

## Deployment Model

**Single-host (default for early projects):**

```
nginx ──► Project.Api (Docker)
       └► Static React build (Docker / nginx serves static)
       └► PostgreSQL (Docker volume mount or managed DB)
       └► Redis (Docker)
```

**Cloud (when needed):**
- Containerized API to AKS / EKS / Azure Container Apps / Cloud Run.
- Managed PostgreSQL (Azure Database / RDS / Cloud SQL).
- Managed Redis (Azure Cache / ElastiCache / Memorystore).
- Static React assets to a CDN (Cloudflare / Azure Front Door / CloudFront).

The Dockerfile and docker-compose are the same in both cases. Only the orchestrator changes.
