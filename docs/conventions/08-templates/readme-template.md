# README Template

Copy to `README.md` at the repo root. Replace placeholders. Keep current.

---

```markdown
# <Project>

<One-sentence description of what this is.>

<Optional: build status badges, coverage, license.>

## Overview

<2–4 paragraphs:>
- What problem this solves and for whom.
- Core capabilities (3–5 bullet points).
- High-level architecture (one sentence pointing at `docs/architecture/`).

## Tech Stack

| Layer    | Technology                                                       |
|----------|------------------------------------------------------------------|
| Backend  | .NET 10 LTS · ASP.NET Core 10 · EF Core 10 · MediatR · Serilog   |
| Frontend | React 18.3 · TypeScript · Vite · Metronic · RTK Query · Tailwind |
| Database | PostgreSQL 16+                                                   |
| Cache    | Redis 7                                                          |
| Infra    | Docker · Docker Compose · GitHub Actions                         |

## Repository Structure

```
.
├── <Project>.Core/                # Backend
├── <Project>.Client/              # Frontend
├── docs/                          # Architecture, ADRs, runbooks, conventions
├── docker-compose.yml
└── README.md
```

## Quickstart (Local Dev)

### Prerequisites

- .NET 10 SDK
- Node 22 LTS (use `.nvmrc` with `nvm use`)
- Docker Desktop (or Docker + Compose v2)
- An IDE: Visual Studio 2026, Rider, or VS Code

### Setup

```bash
# Clone
git clone <repo-url>
cd <project>

# Bring up infra (Postgres, Redis)
docker compose up -d postgres redis

# Set required user-secrets (backend)
cd <Project>.Core
dotnet user-secrets init --project src/<Project>.Api
dotnet user-secrets set --project src/<Project>.Api \
  "Jwt:SigningKey" "<your-32-char-or-longer-key>"

# Apply migrations
dotnet ef database update \
  --project src/<Project>.Infrastructure \
  --startup-project src/<Project>.Api

# Run the API (terminal 1)
dotnet run --project src/<Project>.Api
# → API on http://localhost:5000, Swagger at http://localhost:5000/swagger

# Run the frontend (terminal 2)
cd ../<Project>.Client
cp .env.example .env
npm install
npm run dev
# → Web on http://localhost:5173
```

### Default credentials (seeded for dev)

| Role     | Email                | Password    |
|----------|----------------------|-------------|
| Admin    | admin@<project>.local| Admin1234!  |
| Manager  | manager@<project>.local | Manager1234! |

> **Change these immediately for staging / production seeds.**

## Development

### Backend tests

```bash
cd <Project>.Core
dotnet test
```

### Frontend tests

```bash
cd <Project>.Client
npm run typecheck
npm run lint
npm test
npm run test:e2e         # requires backend running
```

### Lint and format

```bash
dotnet format             # backend
npm run lint:fix          # frontend
npm run format            # frontend
```

## Project Documentation

- **Architecture**: `docs/architecture/solution-overview.md`
- **Conventions**: `docs/conventions/` — opinionated rules for backend, frontend, DB, API, security, DevOps
- **ADRs**: `docs/decisions/` — significant decisions with rationale
- **Runbooks**: `docs/runbooks/` — deploy, rollback, restore, incident response
- **API**: live at `/swagger` when the API is running

## Conventions

This repo follows the conventions in `docs/conventions/`. AI tools and human contributors must read them before generating or reviewing code.

## Deployment

See `docs/runbooks/deployment.md` for the full procedure. CI builds Docker images on every merge to `main` and auto-deploys to staging.

## Contributing

1. Branch from `main`: `git checkout -b feat/<short-name>`.
2. Follow the conventions; add tests.
3. Open a PR. Use the [PR template](.github/pull_request_template.md).
4. CI must be green; one approval required.
5. Merged via squash; branch auto-deleted.

Detail: `docs/conventions/07-standards/01-git-and-pull-requests.md`.

## Support

- Issues: GitHub Issues (or your tracker)
- Chat: <slack channel / discord / etc.>
- On-call: `docs/runbooks/on-call.md`

## License

<MIT / Apache-2.0 / Proprietary / etc.>
```
