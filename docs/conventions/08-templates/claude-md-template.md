# CLAUDE.md Template

Copy this to the root of your project as `CLAUDE.md`. Replace placeholders. AI tools (Claude Code, Copilot, Cursor) read this file to understand the project.

---

```markdown
# <Project>

## Project Overview

<1–3 sentences: what this project does, who uses it, what makes it interesting.>

This repo contains the backend (.NET 10 Clean Architecture API) and frontend (React 18.3 + Vite + Metronic) for <project>.

## Tech Stack

- **Backend**: .NET 10 LTS · C# 14 · ASP.NET Core 10 (Minimal APIs) · EF Core 10 · MediatR · FluentValidation · Serilog · PostgreSQL 16+ · Redis 7
- **Frontend**: React 18.3 · TypeScript 5.6+ · Vite · Metronic React Vite Template · Redux Toolkit + RTK Query · React Hook Form + Zod · Tailwind CSS v4
- **Infra**: Docker · Docker Compose · GitHub Actions · OpenTelemetry

## Project Structure

```
<root>/
├── <Project>.Core/                # Backend
│   ├── src/
│   │   ├── <Project>.Domain/
│   │   ├── <Project>.Application/
│   │   ├── <Project>.Infrastructure/
│   │   ├── <Project>.Api/
│   │   └── <Project>.Shared/
│   ├── tests/
│   ├── Dockerfile
│   └── docker-compose.yml
├── <Project>.Client/              # Frontend
│   ├── src/
│   │   ├── auth/                  # Metronic auth (kept; adapted)
│   │   ├── components/
│   │   ├── modules/<feature>/     # Business modules
│   │   ├── store/
│   │   ├── api/
│   │   └── lib/
│   ├── e2e/
│   └── Dockerfile
├── docs/
│   ├── conventions/               # ← Project conventions library
│   ├── architecture/
│   ├── decisions/                 # ADRs
│   └── runbooks/
└── README.md
```

## Conventions

**This project follows the convention library at `docs/conventions/`. Read it before generating, modifying, or reviewing code.**

Most relevant files:

- Foundation: `docs/conventions/00-foundation/`
- Backend: `docs/conventions/01-backend/`
- Frontend: `docs/conventions/02-frontend/`
- Database: `docs/conventions/03-database/`
- API Design: `docs/conventions/04-api-design/`
- Security: `docs/conventions/05-security/`
- DevOps: `docs/conventions/06-devops/`
- Standards: `docs/conventions/07-standards/`

When the conventions specify a rule, **follow it**. If a rule conflicts with a different best practice, the convention here wins. If a rule is genuinely wrong for this project, open a PR to update the convention rather than deviating in code.

## Architecture (One-Paragraph Summary)

Clean Architecture: Domain → Application → Infrastructure → Api. Domain has zero external deps. Application uses CQRS via MediatR with FluentValidation pipeline behavior. Infrastructure uses EF Core 10 + PostgreSQL with snake_case naming. Api exposes Minimal API endpoints, one group per aggregate. The frontend is feature-sliced under `src/modules/<feature>/`, each module exposes a public surface via `index.ts`. Server state lives in RTK Query; forms in React Hook Form + Zod; styling in Tailwind v4 + Metronic.

## Common Commands

### Backend

```bash
# From <Project>.Core/
dotnet restore
dotnet build
dotnet test
dotnet run --project src/<Project>.Api

# Migrations
dotnet ef migrations add Add_<Subject> \
  --project src/<Project>.Infrastructure \
  --startup-project src/<Project>.Api
dotnet ef database update \
  --project src/<Project>.Infrastructure \
  --startup-project src/<Project>.Api
```

### Frontend

```bash
# From <Project>.Client/
npm install
npm run dev
npm run typecheck
npm run lint
npm test
npm run test:e2e
npm run build
```

### Local stack

```bash
docker compose up -d              # postgres + redis + (optional) api/web
docker compose logs -f api
docker compose down -v            # WIPES volumes — destructive
```

## Required User Secrets (Backend)

```bash
dotnet user-secrets set --project src/<Project>.Api \
  "Jwt:SigningKey" "<32-or-more-character-key>"
dotnet user-secrets set --project src/<Project>.Api \
  "ConnectionStrings:Default" "Host=localhost;Database=<project>_dev;Username=<project>;Password=<project>_dev"
```

## Required Frontend Env (`.env`)

```
VITE_API_BASE_URL=http://localhost:5000
VITE_APP_NAME=<Project>
VITE_ENV=development
```

## Always Do

- Run **tests** before pushing.
- Use **structured logging** (`logger.LogInformation("... {Field}", value)`), never string interpolation.
- Add a **FluentValidation validator** for every command.
- Add a **Zod schema** for every form.
- Use **strongly-typed IDs** (`CustomerId`), not raw `Guid`.
- Use **`IClock`** instead of `DateTime.UtcNow` in domain code.
- Use **`Guid.CreateVersion7()`** for aggregate IDs, not `Guid.NewGuid()`.
- Run **migrations from EF Core**, never write SQL DDL by hand.
- Put **server state in RTK Query**, never copy it into Redux slices.

## Never Do

- **Never commit secrets.** Use user-secrets / GitHub Secrets / env vars.
- **Never `Task.Result` or `.Wait()`.** Always `await`.
- **Never `any` in TypeScript** or `dynamic` in C#.
- **Never use `localStorage` for tokens.** Refresh in HTTP-only cookie; access in memory.
- **Never inline `modelBuilder.Entity<T>()`** in `OnModelCreating`. Use `IEntityTypeConfiguration<T>`.
- **Never default-export non-router components.**
- **Never bundle multiple concerns in one PR.** Split.
- **Never bypass review** even for "trivial" changes.

## Project-Specific Overrides

<List anywhere this project deviates from the convention library, with rationale. Default: none.>

## Module Implementation Order

When adding a new module, follow `docs/conventions/08-templates/module-implementation-checklist.md`. The order is:

1. Domain (entity, value objects, events)
2. Application (commands, queries, validators, handlers, DTOs)
3. Infrastructure (EF config, repository, migration)
4. Api (endpoint group, requests/responses)
5. Frontend (RTK Query slice, schemas, components, pages, route)
6. Tests (unit + integration + E2E happy path)

## Useful Links

- Architecture overview: `docs/architecture/solution-overview.md`
- ADRs: `docs/decisions/`
- Runbooks: `docs/runbooks/`
- API contract: `/swagger` (when API is running locally)
```
