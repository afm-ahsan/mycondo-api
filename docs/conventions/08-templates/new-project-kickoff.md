# New Project Kickoff Playbook

You have a project proposal. You want a working application. This is the ordered playbook that takes you from "I just read the proposal" to "I have a deployable vertical slice" — typically **3–5 days of focused work** for one engineer.

> Throughout: the conventions library (`docs/conventions/` once copied) is the source of truth for *how*. This playbook is the *order*.

---

## Phase 0 — Digest the Proposal (2–3 hours)

Read the proposal **twice**. Once to absorb, once with a notebook. Then fill out the Kickoff Sheet below — short answers, no essays. If you can't answer, that's a question to take back to the client.

### Kickoff Sheet (copy into `docs/kickoff.md`)

```markdown
# <Project> Kickoff

## 1. One-line description
<What is this, in one sentence a stranger could understand?>

## 2. Who uses it
- Primary users: <roles, count>
- Secondary users: <admins, ops, integrators>

## 3. Business goals (3–5 bullets)
- ...

## 4. Modules in scope
| # | Module                | Aggregate(s)              | Notes                         |
|---|-----------------------|---------------------------|-------------------------------|
| 1 | <name>                | <aggregate>               | <complexity, dependencies>    |
| 2 | ...                   | ...                       | ...                           |

## 5. Modules explicitly OUT of scope
- ...

## 6. User roles and permissions (sketch)
| Role        | What they can do                                      |
|-------------|-------------------------------------------------------|
| Admin       | Everything                                            |
| <role>      | <key permissions>                                     |

## 7. Critical workflows (3–5 happy paths)
1. <Actor> does <action> → <observable result>
2. ...

## 8. Non-functional constraints
- Expected scale (users, requests/day, data volume)
- Latency / SLA expectations
- Compliance (HIPAA / GDPR / PCI / SOC 2 / none)
- Languages / locales
- Browsers / devices

## 9. Integrations
- <External system>: <purpose, direction>

## 10. Deadlines / milestones
- <date>: <milestone>

## 11. Open questions for the client
- [ ] ...
- [ ] ...

## 12. First vertical slice (the simplest module)
<Which module touches the fewest other modules and proves the architecture end-to-end?>
```

### Rules

- **The Kickoff Sheet is committed** to the repo as `docs/kickoff.md` and updated as understanding evolves.
- **Open questions count.** If you have zero, you didn't read carefully.
- **Pick the first vertical slice now.** It's almost always the master-data module that nothing depends on (Customers / Products / Categories).

---

## Phase 1 — Naming and Structure (30 min)

Decisions to make once, then never revisit:

| Decision                | Example                                       |
|-------------------------|-----------------------------------------------|
| Repo name               | `<project>` (e.g. `acme-billing`)             |
| Solution name           | `<Project>` PascalCase (e.g. `Acme.Billing`)  |
| Backend folder          | `<Project>Api` or `<Project>.Core`            |
| Frontend folder         | `<Project>Web` or `<Project>.Client`          |
| Default DB schema       | `app` (default) or context name               |
| Service prefix (cache)  | 3-letter code (e.g. `acm`)                    |
| Aggregate names         | One per module — write them down              |

Document these in the Kickoff Sheet. They flow through every other file.

---

## Phase 2 — Repo Bootstrap (3–4 hours)

Create the empty repo and scaffold both projects.

### 2.1 Initialize the repo

```bash
mkdir <project> && cd <project>
git init
```

### 2.2 Drop in the conventions library

```bash
mkdir -p docs
cp -r D:/Workspace/Templates/Template1/project-conventions docs/conventions
```

### 2.3 Bootstrap CLAUDE.md and README.md

Copy the templates and replace placeholders:

```bash
cp docs/conventions/08-templates/claude-md-template.md CLAUDE.md
cp docs/conventions/08-templates/readme-template.md README.md
mkdir -p .github
cp docs/conventions/08-templates/pull-request-template.md .github/pull_request_template.md
```

Fill in `<Project>`, tech versions, contact info. **Don't skip this** — the AI assistant reads `CLAUDE.md` first.

### 2.4 Backend skeleton

Follow `01-backend/01-solution-structure.md` §12. Bootstrap commands:

```bash
mkdir <Project>.Core && cd <Project>.Core
dotnet new sln -n <Project>

# Projects
dotnet new classlib -n <Project>.Domain         -o src/<Project>.Domain
dotnet new classlib -n <Project>.Application    -o src/<Project>.Application
dotnet new classlib -n <Project>.Infrastructure -o src/<Project>.Infrastructure
dotnet new web      -n <Project>.Api            -o src/<Project>.Api
dotnet new classlib -n <Project>.Shared         -o src/<Project>.Shared

# Tests
dotnet new xunit -n <Project>.Domain.UnitTests              -o tests/<Project>.Domain.UnitTests
dotnet new xunit -n <Project>.Application.UnitTests         -o tests/<Project>.Application.UnitTests
dotnet new xunit -n <Project>.Infrastructure.IntegrationTests -o tests/<Project>.Infrastructure.IntegrationTests
dotnet new xunit -n <Project>.Api.IntegrationTests          -o tests/<Project>.Api.IntegrationTests

# References
dotnet add src/<Project>.Application      reference src/<Project>.Domain
dotnet add src/<Project>.Infrastructure   reference src/<Project>.Application
dotnet add src/<Project>.Api              reference src/<Project>.Infrastructure src/<Project>.Application

dotnet sln add (Get-ChildItem -Recurse -Filter *.csproj).FullName
cd ..
```

Create root files:
- `Directory.Build.props` (see `01-backend/01-solution-structure.md` §2).
- `Directory.Packages.props` with the canonical NuGet versions (see `00-foundation/02-tech-stack.md`).
- `global.json` pinning the .NET SDK.
- `.editorconfig`.

### 2.5 Frontend skeleton

```bash
# Copy the Metronic template (don't reinitialize Vite from scratch)
cp -r D:/Workspace/Templates/Template1/react-vite <Project>.Client
cd <Project>.Client

# Clean it
rm -rf node_modules dist .git
# Edit package.json: rename, set version 0.1.0
# Edit index.html: set title

npm install
cp .env.example .env
# Fill in VITE_API_BASE_URL=http://localhost:5000

npm run dev   # confirm the template runs
cd ..
```

Now restructure for our conventions:
- Create `src/modules/` for business modules.
- Create `src/api/`, `src/store/`, `src/lib/`.
- Wire up the Redux store + RTK Query base (`02-frontend/01-project-structure.md` §8 + `02-frontend/03-state-and-data-fetching.md` §1).
- Keep `src/auth/` and `src/components/ui/` as-is — Metronic provides them.

### 2.6 Docker Compose for local infra

Copy the template from `06-devops/01-docker-and-compose.md` §3. Smoke test:

```bash
docker compose up -d postgres redis
docker compose ps   # both healthy
```

### 2.7 First commit

```bash
git add .
git commit -m "chore: initial project bootstrap"
git remote add origin <url>
git push -u origin main
```

---

## Phase 3 — Foundation Infrastructure (½–1 day)

Before writing business code, set up the cross-cutting plumbing once. Otherwise you'll retrofit it module by module.

### Backend foundation

In order:

1. **`AppDbContext`** with `UseSnakeCaseNamingConvention()`. Empty for now. (`01-backend/04-infrastructure-layer.md` §2)
2. **Audit columns interface + interceptor** (`03-database/03-audit-and-soft-delete.md` §2).
3. **Soft-delete interceptor + query filter pattern** (§3).
4. **Domain-event dispatch interceptor** (§5 in same file or `01-backend/04-infrastructure-layer.md` §7).
5. **`IClock` + `SystemClock`** (`01-backend/02-domain-layer.md` §9).
6. **MediatR pipeline behaviors**: `Validation`, `Logging`, `Performance`, `UnhandledException` (`01-backend/03-application-layer.md` §7).
7. **`GlobalExceptionMiddleware`** mapping to ProblemDetails (`01-backend/05-api-layer.md` §4).
8. **`CorrelationIdMiddleware`** (§4 same file).
9. **`IdempotencyMiddleware`** if mutations are non-trivial (§4 + `04-api-design/01-rest-guidelines.md` §6).
10. **Serilog** + structured logging configuration (`01-backend/07-error-handling-and-logging.md` §5).
11. **OpenTelemetry** wiring (§9 same file). Can skip in v0 if no collector exists yet.
12. **Swagger / OpenAPI** with security definitions and operation filter (`04-api-design/04-openapi-swagger.md`).
13. **Health checks** (`/health/live`, `/health/ready`) (`01-backend/05-api-layer.md` §7).
14. **CORS policy** (§8) + **Rate limiting** (§9).
15. **JWT auth setup** with `IOptions<JwtSettings>` and `[Authorize]` defaults (`05-security/01-authentication-and-authorization.md`).
16. **Initial migration** — empty schema with `app`, `auth`, `audit`, `outbox` schemas only.

### Frontend foundation

1. **Redux store + typed hooks** (`02-frontend/01-project-structure.md` §8).
2. **Shared `api` (`createApi`) with auth + correlation interceptors + refresh handling** (`02-frontend/03-state-and-data-fetching.md` §1).
3. **`ApiError` parser + `toUserMessage` helper** (`04-api-design/03-error-responses.md` §5).
4. **`AppShell` layout** using Metronic's existing layout components.
5. **Login / Logout / RequireAuth / RequirePermission** scaffolding (`05-security/01-authentication-and-authorization.md` §10).
6. **Routing skeleton** in `src/routes/app-routes.tsx` with one placeholder dashboard route.
7. **Toast / ErrorBoundary / EmptyState / LoadingSpinner** components in `src/components/feedback/`.
8. **`src/lib/env.ts`** Zod-validated env loader.
9. **Vitest + RTL setup**, MSW handlers folder (`02-frontend/07-testing.md`).

### Smoke test the foundation

- Backend runs, `/health/ready` returns 200, `/swagger` shows the (empty) API.
- Frontend runs, lands on a login page, login fails gracefully (no users yet).
- `dotnet test` passes (no business tests yet, but the test projects compile and at least one architecture test runs).

**Commit.** Don't move on with broken foundation.

---

## Phase 4 — Identity & RBAC (1 day)

This is your second module, but it's foundational so it goes before the business modules.

Build it as a vertical slice using `08-templates/module-implementation-checklist.md`:

- **Domain**: `User`, `Role`, `Permission`, `RefreshToken` aggregates.
- **Application**: `Register`, `Login`, `Refresh`, `Logout`, `ChangePassword`, `Me` use cases.
- **Infrastructure**: EF configurations, password hashing (`BCrypt.Net-Next`), JWT token service, refresh-token storage.
- **Api**: `/api/auth/*` endpoint group (mostly `AllowAnonymous`).
- **Frontend**: login page, refresh flow, `usePermissions` hook, `RequirePermission` route guard.
- **Seed data**: roles (`Admin`, `Manager`, `Viewer`), the permissions inventory, one Admin user.
- **Tests**: unit tests for token issuance, integration tests for login/refresh.

End of Phase 4: you can register a user, log in, get an access token, hit a protected endpoint, refresh when it expires.

---

## Phase 5 — First Business Vertical Slice (1–2 days)

Pick the **simplest** business module from the proposal — usually master data (Customers / Products / Categories). It must touch nothing else first.

Build it end-to-end using `08-templates/module-implementation-checklist.md`. By the end you have:

- Domain aggregate + tests.
- Application commands + queries + validators + tests.
- Infrastructure config + repository + migration.
- Api endpoints + integration tests.
- Frontend RTK Query slice + Zod schema + form + list page + detail page + create/edit pages.
- E2E test for the create → view → edit → delete happy path.
- Working in the browser, end-to-end, with real Postgres.

This slice is the proof that the architecture *holds*. Treat it as a prototype: review your own code against the conventions before going broad.

---

## Phase 6 — Subsequent Modules (rest of the project)

For each remaining module, in dependency order:

1. Copy the Kickoff Sheet's module list, work top to bottom.
2. For each: open `08-templates/module-implementation-checklist.md` and execute it.
3. Stop after every 2–3 modules to refactor anything that's now duplicated. Promote shared utilities to `src/lib/` or `src/components/`.

### Suggested order (typical business app)

```
1. Master data (Customers, Products, Categories)
2. Transactional core (Orders / Quotations / Invoices)
3. Inventory & Stock movement
4. Workflow / approvals
5. HR / Employees / Payroll (if in scope)
6. Expenses / Accounting (if in scope)
7. Reports & Dashboards
8. Notifications / Integrations
```

The order is: **build things others depend on, before things that depend on them.**

---

## Phase 7 — Pre-Production (1–2 days)

When ~80% of the modules ship:

- Run through `05-security/03-security-checklist.md` end-to-end.
- Run through `06-devops/02-environment-configuration.md` to verify staging vs production differences.
- Test the deployment runbook on staging (`docs/runbooks/deployment.md`).
- Test backup/restore (`docs/runbooks/database-backup-restore.md`).
- Performance sanity check: lighthouse on the FE, dotnet-trace on the BE.
- Pen-test the auth surface (or schedule it).

---

## Time Estimates (Solo, Experienced Engineer)

| Phase                                  | Time             |
|----------------------------------------|------------------|
| 0 — Digest proposal                    | 2–3 hours        |
| 1 — Naming and structure               | 30 min           |
| 2 — Repo bootstrap                     | 3–4 hours        |
| 3 — Foundation infrastructure          | ½–1 day          |
| 4 — Identity & RBAC                    | 1 day            |
| 5 — First vertical slice               | 1–2 days         |
| 6 — Per business module thereafter     | 1–2 days each    |
| 7 — Pre-production                     | 1–2 days         |

A 10-module business app: **roughly 3–6 weeks** for a focused solo engineer. Halve it in optimistic scenarios; double it for compliance-heavy domains.

---

## What Goes Wrong (and How to Avoid It)

| Trap                                                              | Antidote                                                              |
|-------------------------------------------------------------------|-----------------------------------------------------------------------|
| Skipping the Kickoff Sheet — building from memory of the proposal | Write it down; the act of writing surfaces gaps                       |
| Skipping foundation infra — "we'll add logging later"             | Module #1 is harder to retrofit observability into than module #10   |
| Building all modules in parallel before proving the first slice   | One slice end-to-end first; *then* breadth                            |
| Naming modules after technical concerns ("UserService") not domain| Domain language: `Customers`, `Quotations`, `AmcContracts`            |
| Treating Phase 4 (auth) as optional for v0                        | Every endpoint that ships without auth is a re-test later             |
| Bypassing the `module-implementation-checklist.md`                | The checklist is what keeps modules uniform                           |
| Adding a 3rd-party library without an ADR                         | The ADR slows you down once; it saves the team forever                |

---

## "I have the proposal — what do I do *right now*?"

1. Read this file.
2. Open the proposal next to a blank `docs/kickoff.md`.
3. Fill the Kickoff Sheet (Phase 0).
4. Make the naming decisions (Phase 1).
5. Bootstrap the repo (Phase 2).
6. Stop and review what you have against the conventions before writing business code.

That's it. Everything after Phase 2 is just "execute the playbook in order."
