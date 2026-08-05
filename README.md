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
└── Dockerfile
```

## Quickstart (Local Dev)

### Prerequisites

- .NET 10 SDK
- Docker Desktop (or Docker + Compose v2)
- An IDE: Visual Studio 2026, Rider, or VS Code with the C# Dev Kit

### Setup

```powershell
# Clone
git clone https://github.com/afm-ahsan/mycondo-api.git
cd mycondo-api

# Bring up infra (Postgres + Redis + Mailhog)
docker compose up -d

# Set required user-secrets
dotnet user-secrets init --project src/MyCondo.Api
dotnet user-secrets set --project src/MyCondo.Api `
  "Jwt:SigningKey" "<your-32-char-or-longer-key>"
dotnet user-secrets set --project src/MyCondo.Api `
  "ConnectionStrings:Default" "Host=localhost;Database=mycondo_dev;Username=mycondo_app;Password=mycondo_dev"

# Apply migrations — as mycondo_migrator (DDL/owner role), NOT mycondo_app (restricted runtime
# role, see appsettings.json/user-secrets above). mycondo_app can't CREATE TABLE by design, so a
# migrator connection string must be supplied for this one command via an env-var override, which
# takes precedence over the user-secrets value above.
$env:ConnectionStrings__Default = "Host=localhost;Database=mycondo_dev;Username=mycondo_migrator;Password=mycondo_migrator_dev"
dotnet ef database update `
  --project src/MyCondo.Infrastructure `
  --startup-project src/MyCondo.Api
Remove-Item Env:\ConnectionStrings__Default

# Optional but recommended: check reserved ports are free before starting (see
# docs/local-development-ports.md for the full multi-project port registry)
pwsh scripts/check-ports.ps1

# Run the API
dotnet run --project src/MyCondo.Api
# → API on https://localhost:7219 (HTTP fallback: http://localhost:5219)
# → OpenAPI 3.1 spec at https://localhost:7219/openapi/v1.json
# → Scalar UI at https://localhost:7219/scalar
```

The companion frontend (`mycondo-web`) reads `VITE_MYCONDO_API_BASE_URL=https://localhost:7219`.

### Default credentials (seeded for dev)

To be populated when the Identity module is seeded.

## Development

### Tests

```powershell
dotnet test
```

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
