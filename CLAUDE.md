# MyCondo API — Claude Code Instructions

This file contains **repository-specific, durable backend engineering rules** for `mycondo-api`.

Global workflow, Git, token-efficiency, communication, verification, and architecture-governance rules are defined in the root `../CLAUDE.md` and apply here.

Load additional documentation only when required by the current task.

---

## 1. Repository Responsibility

`mycondo-api` is the authoritative backend for MyCondo.

Backend ownership includes:

- business rules;
- tenant isolation;
- authorization and permissions;
- financial calculations;
- lifecycle/state transitions;
- validation;
- persistence;
- API contracts;
- integration boundaries.

Do not move authoritative business behavior into `mycondo-web`.

When frontend requirements conflict with backend rules, preserve backend correctness and surface the conflict.

---

## 2. Technology Baseline

Use the existing repository stack and versions unless an approved architectural decision changes them.

Core technologies:

- .NET 10 / C# 14
- ASP.NET Core Minimal APIs
- EF Core + PostgreSQL
- Redis
- SignalR
- Hangfire
- FluentValidation
- ASP.NET Core Identity
- JWT authentication
- OpenAPI
- Serilog
- OpenTelemetry
- xUnit
- NetArchTest

Do not introduce competing frameworks for capabilities already standardized by the project.

---

## 3. Approved Core Libraries & OSS Dependency Policy

MyCondo prefers **actively maintained, widely adopted, community-accessible open-source libraries with permissive licensing**.

For established capabilities, use the project's approved libraries:

- In-process messaging / CQRS dispatch: `Mediator` by martinothamar — MIT licensed.
- Fluent test assertions: `AwesomeAssertions` — Apache-2.0 licensed.

Do not introduce alternative libraries for these capabilities without an explicit architectural decision.

### Dependency Selection

Before introducing any new third-party dependency, verify that it provides meaningful value not already available through:

- the .NET / ASP.NET Core platform;
- an existing MyCondo dependency;
- a small maintainable implementation using platform capabilities.

When an external dependency is justified, prefer libraries that are:

1. open source and community-accessible;
2. permissively licensed;
3. actively maintained;
4. sufficiently adopted and proven;
5. compatible with the current .NET ecosystem;
6. reasonably lightweight;
7. low in vendor or ecosystem lock-in;
8. suitable for commercial SaaS usage without unexpected licensing obligations.

Preferred permissive licenses include:

- MIT;
- Apache-2.0;
- BSD-2-Clause;
- BSD-3-Clause.

Libraries with copyleft, source-available, dual-commercial, usage-restricted, or otherwise materially different licensing require explicit evaluation before adoption.

Do not replace an approved dependency merely because another library is more familiar or popular.

Do not add dependencies for trivial functionality already supported adequately by the platform or existing stack.

Licensing rationale and long-term dependency decisions belong in ADRs rather than being expanded in this file.

---

## 4. Architecture Boundaries

Follow Clean Architecture dependency direction:

`Domain → Application → Infrastructure → Api`

### Domain

Contains:

- aggregates/entities;
- value objects;
- domain rules;
- domain events.

Domain must not depend on Infrastructure or Api concerns.

### Application

Contains feature-oriented:

- commands;
- queries;
- handlers;
- validators;
- DTOs;
- application services;
- authorization abstractions.

Application depends on Domain, not Infrastructure implementation details.

### Infrastructure

Contains:

- EF Core;
- PostgreSQL;
- Redis;
- external service adapters;
- persistence configuration;
- migrations;
- seed infrastructure.

### Api

Contains:

- Minimal API endpoints;
- middleware;
- authentication/authorization composition;
- SignalR hubs;
- request/response transport concerns.

Endpoints orchestrate application behavior. They do not contain authoritative business logic.

---

## 5. Feature-First Vertical Slices

All feature-specific Application code must live beneath its owning feature.

Correct:

```text
Features/
└── Residents/
    ├── Commands/
    │   └── CreateResident/
    └── Queries/
        └── GetResidentById/
```

Do not create global dumping grounds such as:

```text
Commands/
Queries/
Dtos/
Validators/
```

Keep commands, queries, DTOs, validators, mappings, specifications, and related application behavior with their owning business feature.

Prefer existing feature structure over introducing new organizational patterns.

---

## 6. CQRS & Messaging

Use the project's approved in-process messaging library and established CQRS conventions.

Commands represent mutations.

Queries represent reads.

Keep handlers focused on one application use case.

Cross-module interaction must respect existing module boundaries and established event/integration patterns.

Do not introduce direct cross-module coupling merely for convenience.

If a task appears to require violating a module boundary, inspect the applicable ADR before proceeding.

---

## 7. Validation

Every command that accepts external or user-controlled data must have appropriate FluentValidation coverage.

Validation belongs in validators rather than endpoints.

Use domain rules for invariants that must remain true regardless of entry point.

Do not duplicate the same authoritative rule across endpoint, validator, and domain layers without justification.

---

## 8. Identity & Strong Types

Follow existing domain typing conventions.

- Prefer strongly typed IDs where the domain already uses them.
- Do not replace established strongly typed identifiers with raw `Guid`.
- Use `Guid.CreateVersion7()` for new aggregate identifiers where consistent with the existing model.
- Use `IClock` for domain/application time instead of direct `DateTime.UtcNow`.
- Avoid `dynamic`.
- Avoid sync-over-async (`.Result`, `.Wait()`).

Follow existing patterns before introducing new primitives or abstractions.

---

## 9. Multi-Tenancy — Non-Negotiable

Tenant isolation is enforced by both application context and PostgreSQL Row-Level Security.

For tenant-scoped persistence:

- every tenant-scoped table must carry `tenant_id`;
- tenant-aware indexes should lead with `tenant_id` where appropriate;
- new tenant-scoped tables require an explicit RLS policy;
- RLS must be both enabled and forced according to the established repository pattern;
- tenant context must be applied consistently to reads and writes;
- authorization must not be used as a substitute for database tenant isolation.

Never bypass RLS to make a test or feature pass.

Never weaken tenant predicates or tenant context handling for convenience.

### PostgreSQL Role Model

The runtime application role must remain a restricted non-superuser/non-owner role.

Migration/DDL responsibilities and runtime/DML responsibilities remain separated.

Do not configure the normal application runtime to use:

- a PostgreSQL superuser;
- a `BYPASSRLS` role;
- the migration/owner role.

A successful test using a superuser connection does **not** prove RLS isolation.

For RLS-specific implementation or verification, consult the existing PostgreSQL RLS documentation and applicable ADRs rather than reconstructing the pattern.

---

## 10. Tenant Context

Tenant context must be established before tenant-scoped database access.

Follow the existing `TenantContextConnectionInterceptor` and tenant-context conventions.

Background processes, bootstrap operations, and seeders that operate on tenant-scoped data must establish an explicit tenant context.

Do not rely on an HTTP request context when execution occurs outside HTTP.

Global tables without `tenant_id` do not require artificial tenant scoping.

---

## 11. Database & EF Core

Use EF Core migrations for schema evolution.

### Entity Configuration

Use `IEntityTypeConfiguration<T>`.

Do not accumulate entity mappings inline in `OnModelCreating`.

### Naming

Follow existing PostgreSQL naming conventions and schema-per-module architecture.

Do not introduce a new schema or change module ownership without checking the applicable architecture decision.

### Migrations

Migrations are for:

- schema evolution;
- constraints;
- indexes;
- RLS/DCL changes;
- unavoidable migration-time data transformations.

Do not use migrations for ordinary application catalogue/bootstrap seed data.

After a migration has reached a shared or persistent environment, treat it as immutable.

New schema changes create new migrations.

Do not rewrite migration history unless explicitly performing an approved baseline/cutover operation.

Migration names should describe structural intent, for example:

```text
Create...
Add...
Alter...
Rename...
Drop...
```

Do not create `Seed_*`, `*Seed`, or permission-catalogue migrations.

---

## 12. Seed Data & Bootstrap

Application seed data belongs in the established seeding architecture, not EF migration `InsertData` / `HasData`.

Examples include:

- permissions;
- role catalogues;
- role-permission mappings;
- platform bootstrap;
- development/demo bootstrap.

### Catalogue Reconciliation

Catalogue seeders must reconcile using stable natural keys such as:

- permission `Name`;
- role `Code`.

Do not use generated database IDs as catalogue identity.

Do not use broad short-circuit patterns such as:

```csharp
if (await query.AnyAsync())
{
    return;
}
```

when doing so would prevent future catalogue additions from being reconciled.

Catalogue reconciliation should add missing catalogue entries without blindly deleting existing rows.

### Bootstrap

Use the established explicit database-seeding orchestration.

Development/demo seeders must remain protected by an actual runtime Development-environment check.

Do not allow development bootstrap data to execute in Production.

Tenant-scoped seed operations must establish the correct tenant context instead of bypassing RLS.

Preserve existing concurrency protection around startup seeding.

Consult the seed-data architecture documentation when modifying orchestration or bootstrap semantics.

---

## 13. Authorization

Every protected endpoint must use the established permission model.

Do not:

- rely only on UI visibility;
- replace permission checks with role-name checks when permissions are authoritative;
- broaden permissions to fix access problems;
- silently grant administrative capability to resident/user roles.

When adding a capability:

1. reuse an existing permission when semantically correct;
2. otherwise add it to the application permission catalogue;
3. seed/reconcile it through the established permission seeder;
4. assign it only to appropriate roles;
5. protect the endpoint;
6. test authorized and unauthorized access.

Tenant isolation and permission authorization are separate defenses; preserve both.

---

## 14. Minimal API Endpoints

Endpoints should remain thin.

Typical endpoint responsibilities:

- parse/bind transport input;
- obtain required request context;
- dispatch command/query;
- map result to HTTP response.

Do not put:

- domain rules;
- financial calculations;
- persistence queries;
- state-machine logic;
- tenant-isolation logic

directly in endpoint definitions.

Follow existing endpoint-group and OpenAPI conventions.

Do not silently change public request/response contracts.

---

## 15. OpenAPI Contract

Backend OpenAPI is the authoritative client contract.

When an API contract changes:

1. implement and verify the backend change;
2. ensure OpenAPI reflects it;
3. regenerate/update the frontend client using the established workflow;
4. update frontend usage rather than hand-maintaining divergent contracts.

Avoid unnecessary contract churn.

Treat breaking contract changes as explicit design decisions.

---

## 16. Financial Integrity — Non-Negotiable

Financial behavior requires stronger verification than ordinary CRUD.

Preserve these established invariants:

- posted ledger entries are append-only;
- corrections/voids use reversing entries rather than mutation/deletion;
- financial mutation endpoints use the established idempotency mechanism;
- payment allocation follows the established locking/allocation behavior;
- concurrent financial operations must preserve consistency.

Do not simplify locking, idempotency, or ledger behavior without explicit architectural approval.

For financial changes, inspect the applicable billing/payment implementation and ADRs before modifying behavior.

---

## 17. Logging & Observability

Use structured logging.

Correct:

```csharp
logger.LogInformation(
    "Resident {ResidentId} activated for tenant {TenantId}",
    residentId,
    tenantId);
```

Avoid interpolated logging:

```csharp
logger.LogInformation($"Resident {residentId} activated");
```

Never log:

- passwords;
- signing keys;
- tokens;
- sensitive identity values;
- secrets;
- unnecessary personally identifiable information.

Follow existing telemetry conventions instead of introducing parallel observability mechanisms.

---

## 18. Security & Credentials

Never commit real production/shared-environment secrets.

The repository currently has an approved temporary MVP development-credential strategy. Treat it as a narrowly scoped development exception, not a general security convention.

Do not:

- copy development credentials into staging/production configuration;
- reuse development passwords for shared environments;
- broaden the temporary MVP exception;
- remove or weaken production secret-management expectations.

When a task concerns credentials, deployment, staging, production, or onboarding, consult the applicable current ADR rather than relying on remembered values from this file.

Do not repeat credentials in Claude output unless explicitly necessary for the requested task.

---

## 19. Testing Strategy

Follow the root progressive-verification strategy.

Choose tests according to the change.

### Domain / Application

Run affected unit tests first.

Use the project's approved assertion library and existing test conventions.

### Infrastructure / Persistence

Run relevant integration tests when changing:

- EF mappings;
- migrations;
- constraints;
- transaction behavior;
- seeders;
- database interactions.

### Multi-Tenancy

Run real multi-tenancy/RLS verification when changing:

- tenant-scoped entities;
- tenant context;
- RLS policies;
- runtime DB roles;
- cross-tenant queries;
- seed behavior affecting tenant-scoped tables.

Do not consider RLS proven by a superuser-backed local database.

### Architecture

Run architecture tests when changing:

- project dependencies;
- feature structure;
- module boundaries;
- architectural conventions.

### API

Run relevant API integration tests when changing endpoints, authentication, authorization, contracts, or middleware.

Expand to the broader test suite only when scope/risk justifies it or at the final verification gate.

---

## 20. Migration Verification

When adding or modifying persistence structures, verify more than compilation.

As applicable, inspect:

- generated migration;
- model snapshot;
- schema/table ownership;
- constraints;
- indexes;
- tenant columns;
- RLS policy;
- grants/runtime-role access;
- rollback/down behavior;
- compatibility with existing data.

Do not assume EF-generated output is correct merely because generation succeeded.

---

## 21. Implementation Order

For a new backend capability, prefer this sequence when applicable:

1. domain model/invariants;
2. application command/query;
3. validator;
4. handler/application behavior;
5. persistence configuration;
6. migration;
7. authorization/permission;
8. API endpoint;
9. targeted tests;
10. OpenAPI/client-impact verification;
11. broader verification as justified.

Do not rigidly perform irrelevant steps for small changes.

Reuse existing patterns from the closest comparable feature.

---

## 22. Repository Documentation

Do not automatically read the entire convention library.

Consult only documentation relevant to the task.

Typical categories include:

- backend conventions;
- database/migrations;
- API design;
- security;
- architecture decisions;
- module-specific requirements.

If implementation and documentation disagree materially:

1. verify the implementation;
2. identify whether the document is stale or the code violates an approved decision;
3. report the discrepancy;
4. do not silently choose whichever is more convenient.

Do not create duplicate architecture documents.

---

## 23. Local Development & Commands

Use repository-defined commands and current configuration.

Typical backend verification commands include:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/MyCondo.Api
```

Typical migration commands include:

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/MyCondo.Infrastructure `
  --startup-project src/MyCondo.Api

dotnet ef database update `
  --project src/MyCondo.Infrastructure `
  --startup-project src/MyCondo.Api
```

Do not automatically run every command for every task.

Use targeted tests/builds first and escalate verification proportionately.

Before destructive infrastructure commands such as volume deletion, follow the root destructive-operation rule.

For environment-specific connection strings, roles, ports, or credential procedures, inspect the current README/ADR/configuration rather than relying on hard-coded historical instructions here.

---

## 24. Do Not

Do not:

- replace approved core libraries without an explicit architectural decision;
- introduce third-party dependencies without evaluating necessity, maintenance, licensing, and lock-in;
- bypass or disable RLS to make functionality work;
- run the application with a privileged migration/superuser DB role;
- place business logic in Minimal API endpoints;
- place backend business rules in React;
- add ordinary seed data through EF migrations;
- mutate posted ledger entries;
- silently broaden permissions;
- use `.Result` or `.Wait()`;
- use `dynamic` without an exceptional, documented reason;
- inline entity configuration into a growing `OnModelCreating`;
- silently break API contracts;
- introduce generic repositories without justification;
- create cross-module coupling merely for convenience;
- create broad abstractions for a single use case;
- rewrite existing shared migration history;
- perform unrelated refactoring;
- duplicate existing architecture documentation.

---

## 25. Prefer

Prefer:

- permissively licensed, community-accessible OSS dependencies;
- platform capabilities before additional packages;
- existing approved libraries over competing alternatives;
- existing patterns over new abstractions;
- vertical slices over horizontal dumping grounds;
- explicit domain rules over implicit behavior;
- strongly typed domain concepts over primitives;
- backend enforcement over frontend assumptions;
- database isolation plus application authorization;
- targeted inspection over repository-wide exploration;
- targeted tests over repeated full-suite runs;
- evidence over assumptions;
- concise reports over implementation narration.

---

## 26. Task Execution Principle

For each API task:

> **Find the narrowest relevant backend surface, reuse the established pattern, preserve tenant/security/financial invariants, minimize unnecessary dependencies, implement the smallest correct change, verify according to risk, and report only meaningful results.**

Do not spend context rediscovering facts already encoded in the repository unless the current task depends on validating them.