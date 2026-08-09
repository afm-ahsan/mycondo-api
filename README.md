# mycondo-api

Multi-tenant SaaS property-management backend. .NET 10 modular monolith for ARP Flat Owner's Association under proposal MC-PROP-2026-001.

## Overview

MyCondo replaces spreadsheets and paper registers for residential and commercial property management — service charges, invoicing, payments, complaints, vendors, payroll — with one tenant-isolated SaaS platform. This repo is the backend; the SPA lives at https://github.com/afm-ahsan/mycondo-web.

Core capabilities:
- Multi-tenant isolation via PostgreSQL Row-Level Security (forced, not opt-in).
- Append-only double-entry financial ledger; FIFO payment allocation; idempotent mutations.
- Permission-based RBAC (40+ permissions, 10 default roles, building-scoped checks).
- Real-time via SignalR (Redis backplane); background jobs via Hangfire.
- Schema-per-module (18 schemas) preserves a clean microservice extraction path.

High-level architecture: Clean Architecture per layer, modular monolith per module, MediatR-only cross-module communication. See `docs/architecture/solution-overview.md` (TODO) for the full picture.

## Tech Stack

| Layer    | Technology                                                                                  |
|----------|---------------------------------------------------------------------------------------------|
| Backend  | .NET 10 LTS · ASP.NET Core 10 · EF Core 10 · MediatR · FluentValidation · Serilog           |
| Database | PostgreSQL 18 (Row-Level Security; schema-per-module)                                        |
| Cache    | Redis 8.6 (cache + SignalR backplane + idempotency-key store)                                |
| Jobs     | Hangfire (PostgreSQL-backed)                                                                 |
| Auth     | ASP.NET Core Identity 10 + Argon2id + JWT (RS256, 15-min access / 7-day rotating refresh)    |
| Telemetry| OpenTelemetry · Serilog → Seq (dev) / CloudWatch (prod)                                      |
| Infra    | Docker · Docker Compose · GitHub Actions                                                     |

## Repository Structure

```
mycondo-api/
├── MyCondo.sln
├── src/                                  # Domain, Application, Infrastructure, Api, Shared, Modules.*
├── tests/                                # Unit, Integration, MultiTenancy, Architecture
├── tools/MyCondo.DbMigrator/             # Standalone migration runner
├── docs/                                 # conventions/, architecture/, decisions/, runbooks/
├── docker-compose.yml
├── .env.example                          # Docker Compose secrets template (copy to .env)
└── Dockerfile
```

## Developer Environment Matrix

There are three distinct local PostgreSQL execution modes, plus production — each with a different
purpose. **Only Docker Compose, Testcontainers, and Production actually enforce Row-Level Security
and least-privilege**; Native PostgreSQL is a superuser connection and must never be cited as proof
that RLS/tenant isolation works. See `mycondo-docs` ADR-016 for why the role split exists at all, and
the `postgresql-rls` skill (`.claude/skills/postgresql-rls.md`) for the full mechanics.

| Environment | Database | User | Auth source | Purpose | RLS validated |
|---|---|---|---|---|---|
| Native PostgreSQL | `mycondo` | `dev_user` | `appsettings.Development.json` (MVP, see ADR-023) | Daily development, debugging, EF migrations, manual testing | ❌ No — superuser connection |
| Docker Compose | `mycondo_dev` | `mycondo_migrator` (bootstrap/DDL) / `mycondo_app` (runtime) | `.env` (Compose) + connection-string override | Canonical local architecture validation (ADR-016 model) | ✅ Yes |
| Testcontainers | Ephemeral (`mycondo_test` / `mycondo_rls_test`) | Ephemeral `mycondo_migrator`/`mycondo_app`, created per run | Generated in-process | Automated integration tests (`MultiTenancyTests`, `Api.IntegrationTests`) | ✅ Yes |
| Production | Managed DB (not yet provisioned) | Restricted runtime role (TBD when RDS is provisioned) | Deployment secret manager | Production traffic | ✅ Yes (by design; not yet provisioned — see ADR-016's production note) |

## Quickstart (Local Dev)

Pick **one** of the two local paths below — they're independent, not meant to run against the same
database. Both can coexist on one machine (Docker Compose's PostgreSQL is remapped to host port
`5433` specifically so it doesn't collide with a native install on `5432`).

### Prerequisites

- .NET 10 SDK
- An IDE: Visual Studio 2026, Rider, or VS Code with the C# Dev Kit
- Docker Desktop (or Docker + Compose v2) — only required for the Docker Compose path and for
  Testcontainers-backed tests (`MultiTenancyTests`, the `*DbTests` classes in `Api.IntegrationTests`)

### Option A — Native PostgreSQL (day-to-day default)

Fastest path; does **not** validate RLS (see the matrix above). Requires a locally installed
PostgreSQL 18 server (e.g. the native Windows service, or any other local install) with a `mycondo`
database and a `dev_user` role already created — this repo doesn't script that bootstrap, since it's
a pre-existing local server, not something Compose provisions.

**No `dotnet user-secrets` and no environment variables are required for this path.** The connection
string and JWT signing key are already set directly in `appsettings.Development.json` — this is a
deliberate, temporary MVP decision (see `mycondo-docs` ADR-023, "Temporary MVP Development Credential
Strategy"), not the intended production credential architecture. Create the local role/database to
match that committed value exactly:

```sql
-- Run once against your local PostgreSQL 18 server (e.g. via psql):
CREATE ROLE dev_user WITH LOGIN SUPERUSER PASSWORD 'PgDev@1357#';
CREATE DATABASE mycondo OWNER dev_user;
```

```powershell
# Clone
git clone https://github.com/afm-ahsan/mycondo-api.git
cd mycondo-api

# Apply migrations (dev_user is sufficient here — see "Whether table owners bypass RLS" caveat in
# the Developer Environment Matrix above; this is a convenience path, not a least-privilege one)
dotnet ef database update `
  --project src/MyCondo.Infrastructure `
  --startup-project src/MyCondo.Api

# Verify the effective connection resolved correctly without printing secrets
dotnet ef migrations list --project src/MyCondo.Infrastructure --startup-project src/MyCondo.Api

# Optional but recommended: check reserved ports are free before starting (see
# docs/local-development-ports.md for the full multi-project port registry)
pwsh scripts/check-ports.ps1

# Run the API
dotnet run --project src/MyCondo.Api
# → API on https://localhost:7219 (HTTP fallback: http://localhost:5219)
# → OpenAPI 3.1 spec at https://localhost:7219/openapi/v1.json
# → Scalar UI at https://localhost:7219/scalar
```

`PgDev@1357#` is a development-only credential committed to this repository by deliberate MVP
decision. It must never be reused for Staging/Production or any other service, and must be rotated
(not merely deleted from config) before this repository is used against a shared/production database
— see ADR-023.

### Option B — Docker Compose (RLS-validating, ADR-016 model)

Slower to set up, but the one local path that actually exercises RLS and least-privilege the way
production is intended to.

```powershell
# Clone
git clone https://github.com/afm-ahsan/mycondo-api.git
cd mycondo-api

# Copy the secrets template and fill in a real password (never commit .env — it's gitignored)
cp .env.example .env
# edit .env: set POSTGRES_PASSWORD

# Bring up infra (Postgres on host port 5433 + Redis + Mailhog)
docker compose up -d
docker compose ps   # wait for postgres to report healthy

# Jwt:SigningKey is already set in appsettings.Development.json — nothing to configure for it here.
# Override just the connection string for this path — migrator role (DDL/owner), NOT mycondo_app
# (restricted runtime role; it can't CREATE TABLE by design, so migrations must run as
# mycondo_migrator). This path's password is whatever you chose for .env's POSTGRES_PASSWORD (not the
# fixed MVP value from Option A), so it can't live in a committed appsettings file — set it for the
# current shell session instead of via user-secrets:
$env:ConnectionStrings__Default = "Host=localhost;Port=5433;Database=mycondo_dev;Username=mycondo_migrator;Password=<same value as .env's POSTGRES_PASSWORD>"

dotnet ef database update `
  --project src/MyCondo.Infrastructure `
  --startup-project src/MyCondo.Api

# Before running the API day-to-day against this path, switch the override to the restricted
# mycondo_app role instead (Password=mycondo_dev, set by db/init/01_create_app_role.sql on first
# container boot) — running as mycondo_migrator long-term would own the tables and bypass RLS.
$env:ConnectionStrings__Default = "Host=localhost;Port=5433;Database=mycondo_dev;Username=mycondo_app;Password=mycondo_dev"

dotnet run --project src/MyCondo.Api
Remove-Item Env:\ConnectionStrings__Default
```

The companion frontend (`mycondo-web`) reads `VITE_MYCONDO_API_BASE_URL=https://localhost:7219`.

### Default credentials (seeded for dev)

To be populated when the Identity module is seeded.

## Development

### Tests

```powershell
dotnet test
```

`MyCondo.Domain.UnitTests`, `MyCondo.Application.UnitTests`, and `MyCondo.ArchitectureTests` need no
database. `MyCondo.MultiTenancyTests` and the `*DbTests` classes in `MyCondo.Api.IntegrationTests`
are Testcontainers-based — they need a running Docker daemon (Docker Desktop's backend) and manage
their own ephemeral PostgreSQL container and roles independently of `docker-compose.yml`/`appsettings.json`.
They do not read the Native or Docker Compose connection strings above at all.

### Lint and format

```powershell
dotnet format
```

## Project Documentation

- **Conventions**: `docs/conventions/` — opinionated rules; AI tools and humans must read before editing
- **Local ports**: `docs/local-development-ports.md` — the reserved port range for this and sibling local projects
- **Architecture**: `docs/architecture/` (TODO)
- **ADRs**: `docs/decisions/` (TODO)
- **Runbooks**: `docs/runbooks/` (TODO)
- **API**: live at `/scalar` when the API is running

## Conventions

This repo follows the conventions in `docs/conventions/`. Any deviation must be documented as an ADR in `docs/decisions/`.

## Contributing

1. Branch from `main`: `git checkout -b feat/<short-name>`
2. Follow `docs/conventions/`; add tests
3. Open a PR using `.github/pull_request_template.md`
4. CI must be green (build, test, NetArchTest, MultiTenancyTests, security scan)
5. Squash-merge

## License

Proprietary — © 2026 Ajwad Technologies. See proposal §22 for license terms granted to ARP Flat Owner's Association.
