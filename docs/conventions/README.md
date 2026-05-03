# Project Conventions

A reusable, opinionated convention library for building enterprise applications with **.NET 10 (C# 14) + ASP.NET Core Minimal APIs + EF Core 10 + PostgreSQL** on the backend, and **React 18.3 + TypeScript + Vite + Metronic + Redux Toolkit (RTK Query) + Tailwind** on the frontend.

This library is **domain-agnostic**. It describes *how* to structure and write code, not *what* the code is about. Replace placeholders like `<Project>`, `<Module>`, `<Aggregate>`, and `<feature>` with the actual names from your bounded context.

---

## Who This Is For

- New `.NET + React` business automation / ERP / SaaS projects.
- Teams that want a **single source of truth** for architecture, naming, code quality, security, testing, and delivery.
- AI-augmented development with Claude Code, Copilot, or Cursor — these conventions are written so an AI agent can follow them mechanically.

---

## Library Layout

```
project-conventions/
├── README.md                              # This file (the index)
├── 00-foundation/                         # Why and what — read first
├── 01-backend/                            # .NET 10, Clean Architecture, CQRS
├── 02-frontend/                           # React 18, Metronic, RTK Query
├── 03-database/                           # PostgreSQL naming, EF Core, migrations
├── 04-api-design/                         # REST, pagination, error responses
├── 05-security/                           # JWT, RBAC, secrets, OWASP
├── 06-devops/                             # Docker, environment config, CI/CD
├── 07-standards/                          # Git workflow, PR process, docs
└── 08-templates/                          # Reusable file templates
```

---

## Recommended Reading Order

### For a new engineer joining a project
1. `00-foundation/01-overview-and-philosophy.md`
2. `00-foundation/02-tech-stack.md`
3. `00-foundation/03-architecture-overview.md`
4. `01-backend/01-solution-structure.md` (backend devs) **or** `02-frontend/01-project-structure.md` (frontend devs)
5. The relevant coding-standards file for their stack.

### For a lead bootstrapping a new project
1. **`08-templates/new-project-kickoff.md`** — the ordered playbook from "I have a proposal" to "I have a vertical slice".
2. The full `00-foundation/` folder.
3. `08-templates/readme-template.md` and `08-templates/claude-md-template.md`.
4. `08-templates/module-implementation-checklist.md` — defines the order to add features.
5. `06-devops/` — set up Docker, env config, CI before writing business code.

### Before writing any database migration
- `03-database/01-postgresql-naming.md` (mandatory).
- `03-database/03-migrations.md`.

### Before exposing any new API endpoint
- `04-api-design/01-rest-guidelines.md`.
- `04-api-design/03-error-responses.md`.
- `04-api-design/04-openapi-swagger.md`.
- `05-security/01-authentication-and-authorization.md`.

---

## How to Apply These to a New Project

1. **Copy** this `project-conventions/` folder into the new repo as `docs/conventions/` (or keep the same name).
2. **Substitute placeholders** with your actual domain terms:
   - `<Project>` → solution name, e.g. `Acme.Billing` or `SRMedical`.
   - `<Module>` / `<Aggregate>` → bounded context aggregates, e.g. `Customer`, `Invoice`, `Quotation`.
   - `<feature>` → kebab-case feature folder, e.g. `customers`, `quotations`, `sales-orders`.
   - `<schema>` → PostgreSQL schema, e.g. `app`, `billing`, `auth`.
3. **Generate the project skeleton** following `01-backend/01-solution-structure.md` and `02-frontend/01-project-structure.md`.
4. **Wire up `08-templates/claude-md-template.md`** as your project's `CLAUDE.md` so AI assistants follow the same rules.
5. **Implement modules in the order** dictated by `08-templates/module-implementation-checklist.md` — start with foundation (auth, identity), then master data, then business modules.

---

## Core Principles

These principles drive every rule in this library. If a rule does not align with one of these, it should be removed:

1. **Conventions over configuration.** A new engineer should be able to predict where a file lives without asking.
2. **Clean Architecture.** Domain has zero external dependencies. Application orchestrates. Infrastructure adapts. API exposes.
3. **CQRS by default.** Commands and queries are separate paths. Both run through MediatR.
4. **Strict typing end-to-end.** TypeScript strict mode, C# nullable reference types, validation at every boundary.
5. **Validation at boundaries.** FluentValidation in MediatR pipeline. Zod at frontend API boundary.
6. **Idempotency and concurrency.** Mutations carry `Idempotency-Key`. Aggregates carry `Version`. Distributed locks via Redis when truly needed.
7. **Observability is not optional.** Structured logs, correlation IDs, OpenTelemetry from day one.
8. **Security defaults to deny.** All endpoints require authorization unless explicitly marked public.
9. **Tests are written with the code, not after.** New handler → new unit test. New endpoint → new integration test.
10. **Documentation lives with the code.** ADRs in `docs/decisions/`. API contract from OpenAPI. README per project.

---

## When Conventions Conflict

If a rule in this library conflicts with an external library's documented best practice, **the convention here wins** unless the project explicitly updates it. Open a PR against the convention file (with rationale) before deviating.

If two convention files contradict each other, the more specific one wins — and that is a bug to be fixed in the library.

---

## Maintaining This Library

- Each convention file is dated and versioned at the top.
- Major changes require an ADR in `docs/decisions/`.
- The library is **append-only** in spirit: removing a rule needs a stronger justification than adding one.

---

## Tech Stack Snapshot

| Layer            | Technology                                                                   |
|------------------|------------------------------------------------------------------------------|
| Backend runtime  | .NET 10 LTS (C# 14)                                                          |
| Backend web      | ASP.NET Core 10 (Minimal APIs preferred, controllers allowed for legacy)     |
| Backend ORM      | EF Core 10 + `EFCore.NamingConventions` (snake_case) + Dapper for hot reads  |
| Backend patterns | MediatR (CQRS) + FluentValidation + Serilog + OpenTelemetry                  |
| Backend identity | ASP.NET Core Identity + JWT bearer + BCrypt.Net                              |
| Database         | PostgreSQL 16+                                                               |
| Cache / Lock     | Redis 7 (StackExchange.Redis), RedLock when distributed locks are needed     |
| Messaging        | RabbitMQ / Azure Service Bus (Outbox pattern) — when needed                  |
| Frontend         | React 18.3 + TypeScript 5.6+ + Vite                                          |
| Frontend theme   | Metronic React Vite template                                                 |
| Frontend state   | Redux Toolkit + RTK Query (server state) + RHF + Zod (forms)                 |
| Frontend styling | Tailwind CSS v4 + Metronic CSS layers                                        |
| Frontend tests   | Vitest + React Testing Library + Playwright                                  |
| API docs         | Swashbuckle (Swagger / OpenAPI 3.x)                                          |
| DevOps           | Docker + Docker Compose + GitHub Actions                                     |

The detailed version table with rationale is in `00-foundation/02-tech-stack.md`.
