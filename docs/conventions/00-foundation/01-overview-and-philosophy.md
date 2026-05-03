# Overview and Philosophy

This document explains *why* the library exists and the principles every other file is built on. Read this before anything else.

---

## What This Library Is

A set of opinionated conventions for building production-grade business applications with .NET on the backend and React on the frontend. It covers:

- Project structure (folders, projects, references).
- Coding standards (C#, TypeScript, naming, formatting).
- Architectural patterns (Clean Architecture, CQRS, DDD-lite).
- Data layer (PostgreSQL naming, EF Core configuration, migrations).
- API design (REST, pagination, errors, versioning).
- Security (authentication, authorization, secrets).
- DevOps (Docker, environment configuration, CI/CD).
- Process (Git workflow, PRs, documentation).
- Templates (CLAUDE.md, README, ADR, module checklist).

It is **not**:
- A framework. There is no shared NuGet/npm package — these are rules for the code you write.
- A how-to-code-React/.NET tutorial. It assumes intermediate fluency in both.
- A theoretical document. Every rule reflects experience from real projects and is enforceable in CI.

---

## Why a Convention Library

Convention libraries pay back in three places:

1. **Onboarding.** A new engineer opens any module and finds the same shape: a `Domain/`, `Application/`, `Infrastructure/`, `Api/` on the backend; `modules/<feature>/api`, `components`, `hooks`, `pages` on the frontend. Predictability beats cleverness.
2. **Code review.** Reviewers stop arguing about taste. The rule is the rule. If you disagree, change the rule (PR), not the code under review.
3. **AI-augmented development.** Tools like Claude Code, Copilot, and Cursor follow these conventions mechanically when they're written down. A `CLAUDE.md` that points at `project-conventions/` is the difference between an AI that ships and an AI that hallucinates.

---

## Core Principles

### 1. Conventions over configuration
A new engineer should predict where a file lives without asking. Folder names are non-negotiable. Naming patterns are uniform. There is exactly one obvious place for any given concern.

### 2. Clean Architecture, strictly
Dependencies flow inward only.

```
Api / Workers   ──►  Application  ──►  Domain
       │                  ▲
       └──►  Infrastructure  ──┘
```

- `Domain` references nothing.
- `Application` references only `Domain`.
- `Infrastructure` references `Domain` + `Application`.
- `Api` and `Workers` reference all three.

This is enforced in `.csproj` and validated by `NetArchTest` in CI.

### 3. CQRS by default, sparingly applied
Every state change is a command. Every read is a query. Both go through MediatR. A handler does exactly one thing. We do not split read/write databases or run separate event-sourced models — that complexity is opt-in and justified per project.

### 4. DDD-lite
- Aggregate roots own invariants. State changes through methods, not setters.
- Value objects (Email, Money, DateRange) replace primitive obsession.
- Domain events fire on state transitions and dispatch on save.
- We do *not* require event sourcing or full Bounded Contexts unless the project's complexity warrants them.

### 5. Strict typing end-to-end
- C#: nullable reference types on, `TreatWarningsAsErrors=true`, latest analyzers.
- TypeScript: `strict: true`, `noUncheckedIndexedAccess: true`, `verbatimModuleSyntax: true`.
- Validation runs at every boundary: FluentValidation in MediatR pipeline; Zod at the frontend API boundary; database constraints as the last line of defense.

### 6. Server state lives in RTK Query, never duplicated
On the frontend, server data goes through Redux Toolkit Query. Local UI state goes through `useState` or a small Redux slice. Form state goes through React Hook Form. Mirroring server data into local state is forbidden — it creates two sources of truth.

### 7. Idempotency, concurrency, and audit are first-class
- Every mutation accepts an `Idempotency-Key` header.
- Every aggregate has a `Version` column for optimistic concurrency.
- Every entity has `created_at_utc`, `updated_at_utc`, `created_by`, `updated_by`.
- Sensitive entities live in an `audit` schema with a row-level history.

### 8. Observability is not optional
- Structured logs (Serilog) with named templates: `logger.LogInformation("Created {OrderId}", id)`.
- Correlation ID propagated end-to-end via `X-Correlation-Id`.
- OpenTelemetry traces, metrics, and logs ship to OTLP.
- No `console.log`, no `string.Format` in logs.

### 9. Security defaults to deny
- All endpoints require authorization unless explicitly marked `[AllowAnonymous]`.
- Tokens never live in `localStorage`. HTTP-only cookies preferred; in-memory + refresh flow acceptable.
- Inputs validated at the API boundary, never trusted from the client.
- All SQL parameterized (EF Core / Dapper handle this).

### 10. Tests live next to the code
- Backend: each handler ships with a unit test in the matching test project.
- Frontend: each component ships with a `.test.tsx` next to it.
- Integration tests use real PostgreSQL/Redis via Testcontainers — no in-memory fakes for the data layer.
- E2E tests cover the critical user journeys per feature.

### 11. Documentation belongs with the code
- ADRs (Architecture Decision Records) in `docs/decisions/`.
- API contract is the OpenAPI spec, served from the backend, consumed by the frontend.
- README per project, kept current.
- No design documents in chat or in private wikis.

### 12. Reversibility over cleverness
- Migrations are written so they can be rolled forward; destructive changes use the expand → migrate → contract pattern.
- Feature flags for risky launches.
- Deployments are repeatable and idempotent.

---

## Forbidden Anti-Patterns (a partial list)

These are surfaced repeatedly across the convention files. They are surfaced here as well because they are the most common mistakes:

- **Anaemic domain models.** Setters on aggregates. Logic in services that should belong on the aggregate.
- **God services / Manager / Helper / Util** classes. They signal a missing abstraction.
- **`any` in TypeScript** or `dynamic` in C#. Use `unknown` and narrow.
- **`Task.Result` / `.Wait()`** in async code. Always `await`.
- **`useEffect` for derived state**, manual `useMemo`/`useCallback` for non-stability reasons.
- **Tokens in `localStorage`.** Use HTTP-only cookies.
- **Server state in Zustand/Redux slices.** Use RTK Query.
- **Inline `modelBuilder.Entity<T>()` calls** in `OnModelCreating`. Use one `IEntityTypeConfiguration<T>` per entity.
- **`controller-per-action`** style controllers. Use Minimal APIs grouped per aggregate.
- **Plural table names**. Tables are singular (`order`, `invoice`).
- **Generic exceptions / `catch (Exception) { }` swallows.** Catch specific or rethrow with context.

---

## When To Deviate

These conventions are strong defaults, not laws. Deviate when:

1. **A regulatory requirement demands it.** Document with an ADR.
2. **A library's idiom genuinely conflicts.** Document with an ADR; consider whether the library is the right choice.
3. **Performance profiling proves the rule is the bottleneck.** Document with benchmarks.

Do **not** deviate because:
- "It's quicker."
- "I prefer it."
- "I've always done it this way elsewhere."

The convention's value is uniformity. One module that breaks the pattern degrades the predictability of all the others.

---

## How This Library Evolves

1. Spot a recurring issue in code review or in production.
2. Open an issue against the conventions repo.
3. Discuss in the team channel; agree.
4. Open a PR with the rule change + an ADR explaining why.
5. Merge. Existing code is grandfathered until it's touched; new code follows the new rule immediately.

The library should grow slowly. Aggressive churn defeats the purpose.
