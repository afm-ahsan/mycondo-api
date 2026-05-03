# Tech Stack Reference

This document is the **single source of truth** for which versions of which libraries we use, and *why*. When a project bootstraps, it should match this document exactly. When this document changes, the change is intentional, dated, and explained.

> Last verified: 2026-05.

---

## Backend

### Runtime and language

| Item            | Version / Choice                                            | Why                                                                 |
|-----------------|-------------------------------------------------------------|---------------------------------------------------------------------|
| .NET            | **.NET 10 LTS**                                             | Long-term support; latest perf and analyzer improvements.           |
| C#              | **C# 14**                                                   | Primary constructors, collection expressions, field-backed props.   |
| SDK pinning     | `global.json` with `rollForward: latestFeature`             | Reproducible builds; prevents accidental SDK drift.                 |
| Nullable        | `<Nullable>enable</Nullable>` everywhere                    | Eliminates an entire class of bugs at compile time.                 |
| Warnings        | `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`       | Code quality is non-negotiable.                                     |

### Web

| Item            | Version / Choice                                            | Why                                                                 |
|-----------------|-------------------------------------------------------------|---------------------------------------------------------------------|
| ASP.NET Core    | 10                                                          | Matches runtime.                                                    |
| API style       | **Minimal APIs preferred**, controllers allowed for legacy  | Less ceremony; better OpenAPI integration.                          |
| OpenAPI         | **Swashbuckle.AspNetCore** (Swagger UI)                     | Mature, supports XML doc comments, JWT auth UI.                     |
| Health checks   | `Microsoft.Extensions.Diagnostics.HealthChecks`             | `/health/live` and `/health/ready` on every service.                |
| Rate limiting   | `Microsoft.AspNetCore.RateLimiting`                         | Built-in token bucket / fixed window.                               |

### Data layer

| Item                    | Version / Choice                                  | Why                                                                |
|-------------------------|---------------------------------------------------|--------------------------------------------------------------------|
| ORM                     | **EF Core 10**                                    | Aggregate writes; rich change tracking; migrations.                |
| Naming convention       | **EFCore.NamingConventions** (snake_case)         | PostgreSQL idiomatic; no quoted identifiers.                       |
| Provider                | **Npgsql.EntityFrameworkCore.PostgreSQL**         | First-class PostgreSQL features (jsonb, ranges, GIST).             |
| Read-side queries       | **Dapper**                                        | Hand-tuned SQL for hot read paths; faster than EF for projections. |
| Database                | **PostgreSQL 16+**                                | tstzrange, GIST exclusion, generated columns, JSONB, identity.     |
| Cache                   | **Redis 7** via `StackExchange.Redis`             | Distributed cache; pub/sub; backbone for distributed locks.        |
| Distributed lock        | **RedLock.net** (only when truly needed)          | Use sparingly; many problems can be solved with optimistic concurrency. |

### Cross-cutting

| Item                    | Version / Choice                                  | Why                                                                |
|-------------------------|---------------------------------------------------|--------------------------------------------------------------------|
| Mediator (CQRS)         | **MediatR**                                       | Pipeline behaviors for validation/logging/perf.                    |
| Validation              | **FluentValidation**                              | Composable, testable, locale-aware.                                |
| Logging                 | **Serilog** + Console + File sinks                | Structured logging; rich enrichers.                                |
| Telemetry               | **OpenTelemetry** → OTLP                          | Vendor-neutral traces / metrics / logs.                            |
| Mapping                 | Manual `ToDto()` extensions, **not AutoMapper**   | Explicit, debuggable, AOT-friendly.                                |
| Time                    | `IDateTimeProvider` abstraction                   | Testable time. **NodaTime** for date-heavy domains.                |
| Identity                | **ASP.NET Core Identity** + **JWT bearer**        | Standard, integrates with EF Core.                                 |
| Password hashing        | **BCrypt.Net-Next**                               | Industry-standard; adaptive work factor.                           |

### Messaging (when needed)

| Item                    | Version / Choice                                  | Why                                                                |
|-------------------------|---------------------------------------------------|--------------------------------------------------------------------|
| Broker                  | RabbitMQ or Azure Service Bus                     | Either works; pick once per project.                               |
| Pattern                 | Outbox + Idempotent consumer                      | Guarantees at-least-once with exactly-once effects.                |
| Library (RabbitMQ)      | `MassTransit` or `RabbitMQ.Client` directly       | MassTransit if many message types; raw client otherwise.           |

### Testing

| Item                    | Version / Choice                                  | Why                                                                |
|-------------------------|---------------------------------------------------|--------------------------------------------------------------------|
| Test framework          | **xUnit v3**                                      | Modern, fast, parallel by default.                                 |
| Assertions              | **FluentAssertions**                              | Readable failures.                                                 |
| Mocking                 | **NSubstitute**                                   | Cleaner syntax than Moq; lambda-based.                             |
| Integration containers  | **Testcontainers**                                | Real PostgreSQL/Redis/RabbitMQ in tests.                           |
| DB reset between tests  | **Respawn**                                       | Fast, deterministic.                                               |
| API integration         | **WebApplicationFactory**                         | Built-in to ASP.NET Core; full pipeline.                           |
| Architecture tests      | **NetArchTest**                                   | Enforces dependency direction.                                     |

---

## Frontend

### Runtime and language

| Item            | Version / Choice                                            | Why                                                                 |
|-----------------|-------------------------------------------------------------|---------------------------------------------------------------------|
| React           | **18.3** (matches Metronic)                                 | Stable, broad compatibility, no React Compiler yet.                 |
| TypeScript      | **5.6+** with strict mode                                   | Type safety end-to-end.                                             |
| Bundler         | **Vite**                                                    | Fast dev server; modern ESM.                                        |
| Node            | **22 LTS** (pin via `.nvmrc`)                               | Match CI; reproducible.                                             |
| Package manager | **npm** (matches Metronic) — pnpm acceptable per-project    | Match the template the team is using.                               |

### UI / theming

| Item            | Version / Choice                                            | Why                                                                 |
|-----------------|-------------------------------------------------------------|---------------------------------------------------------------------|
| Template        | **Metronic React Vite**                                     | Production-grade theme; layouts, demos, components.                 |
| Styling         | **Tailwind CSS v4**                                         | Utility-first; CSS-first config via `@theme`.                       |
| Primitives      | **shadcn/ui** + **Radix UI** (ships with Metronic)          | Accessible primitives; styled by Tailwind.                          |
| Icons           | **KeenIcons** (Metronic) + **Lucide** for fillers           | Match the template.                                                 |
| Charts          | **ApexCharts** + **Recharts** (Metronic ships both)         | Pick one per chart need.                                            |

### State and data

| Item                    | Version / Choice                                  | Why                                                                |
|-------------------------|---------------------------------------------------|--------------------------------------------------------------------|
| State (server)          | **Redux Toolkit Query (RTK Query)**               | Caching, invalidation, polling out of the box.                     |
| State (client UI)       | **Redux Toolkit slices** for cross-cutting; `useState` for local | Predictable; devtools integration.                          |
| Forms                   | **React Hook Form** + **Zod** + `@hookform/resolvers` | Performance; schema validation matches backend.                |
| Routing                 | **React Router** (or whatever Metronic ships with that project) | Familiar; lazy routes.                                       |
| HTTP                    | **fetch** wrapped in RTK Query's `baseQuery`      | No axios needed.                                                   |
| Date                    | **date-fns** (UTC by default)                     | Tree-shakable; no moment.                                          |
| i18n                    | **react-intl** or **i18next** (Metronic ships react-intl) | Pick one per project; do not mix.                            |

### Quality

| Item                    | Version / Choice                                  | Why                                                                |
|-------------------------|---------------------------------------------------|--------------------------------------------------------------------|
| Linter                  | **ESLint** (flat config) + Prettier               | Standard tooling.                                                  |
| Tests (unit/component)  | **Vitest** + **React Testing Library**            | Fast; same loader as Vite.                                         |
| Tests (E2E)             | **Playwright**                                    | Multi-browser; auto-waiting.                                       |
| API mocking             | **MSW**                                           | Same handlers in tests, Storybook, dev.                            |
| OpenAPI codegen         | **openapi-typescript** (types) + RTK Query codegen | Backend OpenAPI is the source of truth.                           |

---

## Database

| Item                    | Version / Choice                                  | Notes                                                              |
|-------------------------|---------------------------------------------------|--------------------------------------------------------------------|
| Engine                  | **PostgreSQL 16+**                                | Use `timestamptz`, `jsonb`, `tstzrange`, GIST exclusion.           |
| Naming                  | snake_case via `EFCore.NamingConventions`         | No quoted identifiers, ever.                                       |
| Schemas                 | `app`, `auth`, `audit`, `outbox`, per-context     | Avoid dumping in `public`.                                         |
| IDs                     | **UUIDv7** (sortable) for aggregate roots         | Generated client-side via `Guid.CreateVersion7()`.                 |
| Money                   | `numeric(19, 4)`                                  | Never `float` or `real`.                                           |
| Timestamps              | `timestamptz`                                     | Suffix columns `_at_utc`.                                          |
| Migrations              | EF Core `dotnet ef migrations add`                | Descriptive names; comment block at top.                           |

Detail rules: `03-database/01-postgresql-naming.md`.

---

## DevOps

| Item                    | Version / Choice                                  | Notes                                                              |
|-------------------------|---------------------------------------------------|--------------------------------------------------------------------|
| Containers              | **Docker** (multi-stage)                          | One Dockerfile per project (api, web).                             |
| Local orchestration     | **Docker Compose**                                | Brings up app + Postgres + Redis + tools.                          |
| CI                      | **GitHub Actions**                                | One workflow per stack.                                            |
| Static analysis         | Roslyn analyzers (.NET) + ESLint + tsc            | Run in CI; PRs blocked on warnings.                                |
| Container registry      | GHCR / ACR / ECR (project-specific)               | Use one per environment.                                           |
| Secrets                 | dotnet user-secrets (local), Key Vault / GitHub Secrets (CI/prod) | Never committed.                                       |

---

## Versioning Policy

- **Major version pinning** in `Directory.Packages.props` (backend) and `package.json` (frontend) — exact, not ranges.
- **Renovate** or **Dependabot** opens PRs weekly. PRs must pass CI to merge.
- **One framework upgrade per quarter** as a planned task with its own ADR.
- **Deprecated dependencies are removed** within one quarter of identification.

---

## When You Need Something Not Listed

1. Check whether the existing stack already covers it.
2. If not, propose a library in an ADR (`docs/decisions/adr-NNN-<topic>.md`).
3. The ADR includes: problem, options considered, choice, trade-offs, expiration date for review.
4. If accepted, add it here.

The bias is **toward the existing stack**. Every additional dependency is a long-term tax.
