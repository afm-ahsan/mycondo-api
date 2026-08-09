# MyCondo — API

## Project Overview

MyCondo is a multi-tenant SaaS building automation and property management platform delivered to ARP Flat Owner's Association under proposal MC-PROP-2026-001 (fixed-price BDT 2,50,000, 24 weeks). This repo is the **backend**: a Clean Architecture + modular monolith .NET 10 API serving 14+ modules across 19 PostgreSQL schemas (18 approved 2026-07-28 per ADR-004, plus `operations` added 2026-08-07 for Slice H — see the ADR-004 addendum), with strict tenant isolation via Row-Level Security.

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
- **In-process messaging**: `Mediator` (martinothamar, MIT) — not the commercially-licensed `MediatR` (ADR-002)
- **Validation**: FluentValidation 11
- **Auth**: ASP.NET Core Identity 10 + Argon2id (Konscious) + JWT Bearer (RS256, 15-min access / 7-day rotating refresh)
- **API docs**: `Microsoft.AspNetCore.OpenApi` (OpenAPI 3.1) + Scalar UI
- **Logging**: Serilog (structured JSON) → Seq (dev) / CloudWatch (prod)
- **Telemetry**: OpenTelemetry (.NET 10 native)
- **Tests**: xUnit + `AwesomeAssertions` (not `FluentAssertions`, ADR-003) + NetArchTest

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

Clean Architecture: Domain → Application → Infrastructure → Api. Domain has zero external deps. Application uses CQRS via `Mediator` (martinothamar, MIT — not the commercially-licensed `MediatR`, see ADR-002) with FluentValidation pipeline behavior. Infrastructure uses EF Core 10 + PostgreSQL 18 with **schema-per-module** (19 schemas, snake_case naming — see ADR-004 and its 2026-08-07 addendum). Api exposes Minimal API endpoints, one group per aggregate, every endpoint declares `[RequirePermission(...)]` or `[AllowAnonymous]`. Modules communicate **only** via domain events — no direct cross-module project references (enforced by NetArchTest).

## Multi-tenancy (non-negotiable)

- Every tenant-scoped table has a `tenant_id UUID NOT NULL` — implemented today in the `identity` schema tables.
- **RLS is enabled and forced, as of 2026-07-28 (Wave 1 Slice 2).** `rls_<table>_tenant_isolation` policies enforce `tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid` (note the `NULLIF` — a direct cast raises a Postgres error when the GUC is `''`) on `users`, `roles`, `role_assignments`, `refresh_tokens`, and `role_permissions`, with RLS `ENABLED` and `FORCED`. See `mycondo-api/.claude/skills/postgresql-rls.md` for the exact pattern to copy when a new tenant-scoped table needs it — RLS is per-table, not automatic.
- **The connecting role matters as much as the policy.** Postgres superusers and `BYPASSRLS` roles always bypass RLS, `FORCE` or not. Under the Docker Compose / Testcontainers / production model (ADR-016), the app runs as `mycondo_app` — non-superuser, non-owner (owns nothing; DML-only privileges granted via `Grant_App_Role_Runtime_Privileges`) — never as `mycondo_migrator` (the DDL/owner role, used only for `dotnet ef database update`). This split exists *because* the original single-role setup used a Postgres superuser for everything, which silently defeated RLS end-to-end until the first real Testcontainers run caught it (see ADR-016 in `mycondo-docs`). If you ever see the app connecting with a role that owns its own tables or is a superuser, RLS is not actually protecting anything, regardless of what the migration history says.
- **Native PostgreSQL local dev is a separate, explicitly non-RLS-proving path.** It connects as `dev_user`, a superuser on that instance — convenient for day-to-day work, but RLS/tenant-isolation must never be considered validated by anything run against it. See README.md's Developer Environment Matrix for the full comparison across Native/Docker Compose/Testcontainers/Production.
- `TenantContextConnectionInterceptor` sets `app.current_tenant_id` on every connection open (reads and writes alike), not just in `SaveChangesAsync` — this closes the read-path sequencing risk that `mycondo-docs/02-architecture/TARGET_ARCHITECTURE.md` §4 previously flagged as a blocker.
- Composite indexes on tenant-scoped tables always lead with `tenant_id`.
- `MyCondo.MultiTenancyTests` has real cross-tenant tests (Testcontainers-backed), executed against a real Docker daemon and passing against the restricted `mycondo_app` role — not just compile-verified.

## Seed Data & Bootstrap Architecture (non-negotiable)

- **Migrations are for schema evolution and genuine migration-time data transformations** (e.g. a
  backfill on existing rows during a schema change). **Ordinary application seed data — permissions,
  role catalogues, role-permission mappings, SuperAdmin/platform bootstrap, development/demo data —
  belongs in dedicated seeders under `src/MyCondo.Infrastructure/Seed/` and
  `src/MyCondo.Application/Common/Services/*CatalogueSeeder.cs`, never in EF Core `InsertData`/`HasData`.**
  The global permission catalogue was migration-seeded through 14 historical migrations
  (`Seed_Permission_Catalogue` and its successors); those files are preserved as historical record and
  are **not** edited — going forward, new permissions are added to
  `MyCondo.Application.Common.Authorization.PermissionCatalogue` and reconciled by `PermissionSeeder`,
  never via a new seed migration.
- **Every seeder reconciles by a stable natural key** — a permission's `Name`, a role's `Code` — never
  a database-generated ID, and never the classic `if (await X.AnyAsync()) return;` short-circuit (that
  pattern permanently blocks a catalogue entry added later from ever reaching an already-bootstrapped
  environment; see `docs/conventions/03-database/02-ef-core-and-migrations.md` §7 for the full
  rationale). Reconciliation only ever creates what's missing — it never deletes or alters an existing
  row not in the current catalogue.
- **One explicit orchestration entry point**: `await app.Services.SeedDatabaseAsync(app.Environment)` in
  `Program.cs` (`MyCondo.Infrastructure.Seed.DatabaseSeederExtensions`). Order: (1) the global
  permission catalogue, every environment; (2) Development-only bootstrap/demo seeders
  (`PlatformBootstrapSeeder`, `ArpDevelopmentBootstrapSeeder`, `DevelopmentTenantSeeder`), behind a hard
  runtime `IHostEnvironment.IsDevelopment()` check — not just conditional DI registration — so a future
  change can't accidentally let dev/demo data run in Production. `MyCondo.DbMigrator`'s tenant-bootstrap
  CLI command seeds permissions the same way, since it may run against a database the API has never
  started against yet.
- **Tenant-scoped catalogue seeding needs an explicit tenant context**, since it runs outside any HTTP
  request. The established pattern is a small, private `ITenantContextAccessor` implementation fixed to
  one tenant ID, wired into a purpose-built `MyCondoDbContext` (see `ArpDevelopmentBootstrapSeeder`,
  `MyCondo.DbMigrator`, and the test suites' `PostgresApiFactory.CreateDbContextForTenant`) — RLS stays
  fully enforced; the seeder is just correctly told which tenant it's writing as. Genuinely global,
  tenant-less tables (`identity.permissions`, `platform.*`, `tenancy.tenants` — no `tenant_id`, no RLS
  policy) need no tenant context at all.
- **Concurrent startup**: `SeedDatabaseAsync` wraps its sequence in a Postgres session-level advisory
  lock so multiple API instances starting at once serialize through seeding rather than racing.
- **Bootstrap identities are not catalogues.** A SuperAdmin/platform-administrator identity is a true
  singleton (guarded by an existence check on its own unique identity, e.g. email) — but any *grants*
  attached to it (e.g. the Platform SuperAdmin's `platform.*` permissions) still reconcile by natural
  key on every run, so a permission added to the catalogue later still reaches an already-bootstrapped
  SuperAdmin.

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

**Native PostgreSQL path:** `dev_user` can already `CREATE TABLE` (it's a superuser on that instance —
see "Multi-tenancy" below), so `dotnet ef database update` works directly against the connection
string already set in user-secrets, no override needed.

**Docker Compose path:** `dotnet ef database update` needs the `mycondo_migrator` (DDL/owner) role,
not the `mycondo_app` role the app runs as day-to-day — `mycondo_app` is intentionally restricted to
DML and cannot `CREATE TABLE`. Override the connection string just for this command (port `5433`, not
`5432`):

```powershell
dotnet ef migrations add Add_<Subject> `
  --project src/MyCondo.Infrastructure `
  --startup-project src/MyCondo.Api

$env:ConnectionStrings__Default = "Host=localhost;Port=5433;Database=mycondo_dev;Username=mycondo_migrator;Password=<same value as .env's POSTGRES_PASSWORD>"
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

Values differ by which local path you're using — see README.md's Developer Environment Matrix and
Quickstart for the full walkthrough of both.

```powershell
dotnet user-secrets set --project src/MyCondo.Api `
  "Jwt:SigningKey" "<32-or-more-character-key>"

# Native PostgreSQL path (day-to-day, does not validate RLS):
dotnet user-secrets set --project src/MyCondo.Api `
  "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=mycondo;Username=dev_user;Password=<your-local-password>"

# Docker Compose path (ADR-016 model, validates RLS) — note port 5433, not 5432:
dotnet user-secrets set --project src/MyCondo.Api `
  "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=mycondo_dev;Username=mycondo_app;Password=mycondo_dev"
```

## Mandatory Git Branch Policy

Before modifying code for any new feature, bug fix, refactor, architecture change, or other meaningful development task:

1. Inspect `git status`, current branch, and recent history.
2. Preserve unrelated/uncommitted work.
3. Fetch the latest remote state.
4. Create or switch to a dedicated task branch from the latest appropriate `main`.
5. Verify the branch and base before editing code.

Never develop directly on `main` unless explicitly instructed. Never use an unrelated currently checked-out feature branch as the base for new work — the branch that happens to be checked out is not automatically the correct one.

Branch from something other than `main` only when the task has a genuine unmerged dependency; report `DEPENDENT BRANCH REQUIRED` and explain the dependency before proceeding, rather than silently stacking a new branch on top of unmerged work.

Branch creation is mandatory pre-flight work, not optional — but it does not by itself authorize commit, push, merge, rebase, force-push, branch deletion, or PR creation. Those require task-specific authorization.

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
- **Schema-per-module** (19 schemas as of the ADR-004 addendum adding `operations` for Slice H) instead of the convention's single `app` schema default. Per proposal §06 / MyCondo.md §06: surfaces module ownership at the DB layer and eases future microservice extraction.
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
