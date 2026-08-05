# MyCondo — API

## Project Overview

MyCondo is a multi-tenant SaaS building automation and property management platform delivered to ARP Flat Owner's Association under proposal MC-PROP-2026-001 (fixed-price BDT 2,50,000, 24 weeks). This repo is the **backend**: a Clean Architecture + modular monolith .NET 10 API serving 14 modules across 18 PostgreSQL schemas, with strict tenant isolation via Row-Level Security.

The companion frontend repo is at https://github.com/afm-ahsan/mycondo-web.

**Governance baseline:** see `../mycondo-docs/02-architecture/CURRENT_STATE_ASSESSMENT.md`,
`TARGET_ARCHITECTURE.md`, and `Architecture_Decision_Register.md` (established 2026-07-28, Wave 0)
for the current verified state of this repo, open architecture decisions, and the delivery backlog.
Several items below reflect the *target* state, not the current one — each is annotated where that's
the case.

## Tech Stack

- **Runtime**: .NET 10 LTS · C# 14 · ASP.NET Core 10 (Minimal APIs)
- **Persistence**: EF Core 10 · PostgreSQL 18 · Npgsql.EntityFrameworkCore.PostgreSQL 10.x
- **Cache & real-time**: Redis 8.6 · StackExchange.Redis · SignalR (Redis backplane)
- **Background jobs**: Hangfire (PostgreSQL-backed)
- **In-process messaging**: MediatR
- **Validation**: FluentValidation 11
- **Auth**: ASP.NET Core Identity 10 + Argon2id (Konscious) + JWT Bearer (RS256, 15-min access / 7-day rotating refresh)
- **API docs**: `Microsoft.AspNetCore.OpenApi` (OpenAPI 3.1) + Scalar UI
- **Logging**: Serilog (structured JSON) → Seq (dev) / CloudWatch (prod)
- **Telemetry**: OpenTelemetry (.NET 10 native)
- **Tests**: xUnit + FluentAssertions + NetArchTest

## Project Structure

```
mycondo-api/
├── MyCondo.sln
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── .editorconfig
├── Dockerfile
├── docker-compose.yml
├── .github/
│   └── workflows/
├── src/
│   ├── MyCondo.Domain/                     # Entities, value objects, domain events. Zero deps.
│   ├── MyCondo.Application/                # MediatR handlers, DTOs, validators. Depends on Domain.
│   ├── MyCondo.Infrastructure/             # EF Core, Redis, S3, email/SMS adapters.
│   ├── MyCondo.Api/                        # ASP.NET Core entry, SignalR hubs, middleware.
│   ├── MyCondo.Shared/                     # Cross-cutting types (no business logic).
│   └── (later) MyCondo.Modules.<Module>/   # Each module added in its own phase.
├── tests/
│   ├── MyCondo.Domain.UnitTests/
│   ├── MyCondo.Application.UnitTests/
│   ├── MyCondo.Infrastructure.IntegrationTests/
│   ├── MyCondo.Api.IntegrationTests/
│   ├── MyCondo.MultiTenancyTests/          # RLS isolation validation
│   └── MyCondo.ArchitectureTests/          # NetArchTest enforces module boundaries
├── tools/
│   └── MyCondo.DbMigrator/                 # Standalone migration runner
└── docs/
    ├── conventions/                         # Convention library (duplicated from template)
    ├── architecture/
    ├── decisions/                           # ADRs
    └── runbooks/
```

## Conventions

**This project follows the convention library at `docs/conventions/`. Read it before generating, modifying, or reviewing code.**

Most relevant files:
- Foundation: `docs/conventions/00-foundation/`
- Backend: `docs/conventions/01-backend/`
- Database: `docs/conventions/03-database/`
- API Design: `docs/conventions/04-api-design/`
- Security: `docs/conventions/05-security/`
- DevOps: `docs/conventions/06-devops/`
- Standards: `docs/conventions/07-standards/`

When the conventions specify a rule, **follow it**. Project-specific overrides are listed below.

## Architecture (one-paragraph summary)

Clean Architecture: Domain → Application → Infrastructure → Api. Domain has zero external deps. Application uses CQRS via MediatR with FluentValidation pipeline behavior. Infrastructure uses EF Core 10 + PostgreSQL 18 with **schema-per-module** (18 schemas, snake_case naming). Api exposes Minimal API endpoints, one group per aggregate, every endpoint declares `[RequirePermission(...)]` or `[AllowAnonymous]`. Modules communicate **only** via MediatR domain events — no direct cross-module project references (enforced by NetArchTest).

## Multi-tenancy (non-negotiable)

- Every tenant-scoped table has a `tenant_id UUID NOT NULL` — implemented today in the `identity` schema tables.
- **RLS is enabled and forced, as of 2026-07-28 (Wave 1 Slice 2).** `rls_<table>_tenant_isolation` policies enforce `tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid` (note the `NULLIF` — a direct cast raises a Postgres error when the GUC is `''`) on `users`, `roles`, `role_assignments`, `refresh_tokens`, and `role_permissions`, with RLS `ENABLED` and `FORCED`. See `mycondo-api/.claude/skills/postgresql-rls.md` for the exact pattern to copy when a new tenant-scoped table needs it — RLS is per-table, not automatic.
- **The connecting role matters as much as the policy.** Postgres superusers and `BYPASSRLS` roles always bypass RLS, `FORCE` or not. The app runs as `mycondo_app` — non-superuser, non-owner (owns nothing; DML-only privileges granted via `Grant_App_Role_Runtime_Privileges`) — never as `mycondo_migrator` (the DDL/owner role, used only for `dotnet ef database update`). This split exists *because* the original single-role setup used a Postgres superuser for everything, which silently defeated RLS end-to-end until the first real Testcontainers run caught it (see the ADR recording this in `mycondo-docs`). If you ever see the app connecting with a role that owns its own tables or is a superuser, RLS is not actually protecting anything, regardless of what the migration history says.
- `TenantContextConnectionInterceptor` sets `app.current_tenant_id` on every connection open (reads and writes alike), not just in `SaveChangesAsync` — this closes the read-path sequencing risk that `mycondo-docs/02-architecture/TARGET_ARCHITECTURE.md` §4 previously flagged as a blocker.
- Composite indexes on tenant-scoped tables always lead with `tenant_id`.
- `MyCondo.MultiTenancyTests` has real cross-tenant tests (Testcontainers-backed), executed against a real Docker daemon and passing against the restricted `mycondo_app` role — not just compile-verified.

## Financial integrity (non-negotiable)

- Append-only double-entry ledger in `payments.ledger_entries`. **No deletes** — voids create reversing entries.
- All financial mutations (POST on billing/payments) require `X-Idempotency-Key`; validated against `payments.idempotency_keys`.
- Payment allocation = FIFO with `SELECT … FOR UPDATE` row locking.

## Common Commands

### Build, test, run

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/MyCondo.Api
# → https://localhost:7219 (HTTP fallback http://localhost:5219) — see docs/local-development-ports.md
```

### Migrations

`dotnet ef database update` needs the `mycondo_migrator` (DDL/owner) role, not the `mycondo_app`
role the app runs as — `mycondo_app` is intentionally restricted to DML and cannot `CREATE TABLE`
(see "Multi-tenancy" below). Override the connection string just for this command:

```powershell
dotnet ef migrations add Add_<Subject> `
  --project src/MyCondo.Infrastructure `
  --startup-project src/MyCondo.Api

$env:ConnectionStrings__Default = "Host=localhost;Database=mycondo_dev;Username=mycondo_migrator;Password=mycondo_migrator_dev"
dotnet ef database update `
  --project src/MyCondo.Infrastructure `
  --startup-project src/MyCondo.Api
Remove-Item Env:\ConnectionStrings__Default
```

### Local infra

```powershell
docker compose up -d                  # postgres + redis + mailhog
docker compose logs -f api
docker compose down -v                # WIPES volumes — destructive
```

## Required user-secrets (backend)

```powershell
dotnet user-secrets set --project src/MyCondo.Api `
  "Jwt:SigningKey" "<32-or-more-character-key>"
dotnet user-secrets set --project src/MyCondo.Api `
  "ConnectionStrings:Default" "Host=localhost;Database=mycondo_dev;Username=mycondo_app;Password=mycondo_dev"
```

## Always Do

- Run **tests** before pushing.
- Use **structured logging** (`logger.LogInformation("... {Field}", value)`), never string interpolation.
- Add a **FluentValidation validator** for every command.
- Use **strongly-typed IDs** (`CustomerId`), not raw `Guid`.
- Use **`IClock`** instead of `DateTime.UtcNow` in domain code.
- Use **`Guid.CreateVersion7()`** for aggregate IDs.
- Run **migrations from EF Core**, never write SQL DDL by hand.

## Never Do

- **Never commit secrets.** Use user-secrets / GitHub Secrets / env vars (`MYCONDO_*`).
- **Never `Task.Result` or `.Wait()`.** Always `await`.
- **Never `dynamic`.**
- **Never inline `modelBuilder.Entity<T>()`** in `OnModelCreating`. Use `IEntityTypeConfiguration<T>`.
- **Never bundle multiple concerns in one PR.** Split.

## Project-Specific Overrides

These deviate from the conventions library; an ADR will be added to `docs/decisions/` before Phase 2 work begins.

- **Two-repo layout** (this repo + `mycondo-web`) instead of the convention's monorepo with sibling `MyCondo.Core/` + `MyCondo.Client/` folders. Per proposal §03 ("two clean repos, lowercase, hyphenated, role-based, independent deployment").
- **Schema-per-module** (18 schemas) instead of the convention's single `app` schema default. Per proposal §06 / MyCondo.md §06: surfaces module ownership at the DB layer and eases future microservice extraction.
- **PostgreSQL 18 + Redis 8.6** instead of the convention's PG 16 / Redis 7 mention. Per proposal §06 — current stable releases.

## Module Implementation Order

When adding a new module, follow `docs/conventions/08-templates/module-implementation-checklist.md`. Phased order locked in `docs/kickoff.md` (companion document, lives separately):

1. Domain (entity, value objects, events)
2. Application (commands, queries, validators, handlers, DTOs)
3. Infrastructure (EF config, repository, migration)
4. Api (endpoint group, requests/responses)
5. Frontend (in `mycondo-web` — RTK Query slice, schemas, components, pages, route)
6. Tests (unit + integration + multi-tenancy + E2E happy path)

## Useful Links

- Frontend repo: https://github.com/afm-ahsan/mycondo-web
- API contract: `/scalar` (when API is running locally)
- Architecture overview: `docs/architecture/solution-overview.md` (TODO)
- ADRs: `docs/decisions/` (TODO)
- Runbooks: `docs/runbooks/` (TODO)
