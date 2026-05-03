# Documentation Standards

Documentation is part of the product. It's reviewed in the same PRs as the code that ships with it.

---

## 1. What Lives Where

| Location                               | Content                                                         |
|----------------------------------------|-----------------------------------------------------------------|
| `README.md` (per project / repo root)  | What this is, how to run it, where to find things               |
| `CLAUDE.md` (root)                     | AI assistant memory; points at conventions                      |
| `docs/architecture/`                   | High-level architecture, data flow diagrams                     |
| `docs/decisions/`                      | Architecture Decision Records (ADRs)                            |
| `docs/runbooks/`                       | Operational procedures (deploy, restore, incident response)     |
| `docs/database/`                       | Schema overview, migration history per release                  |
| `docs/api/` (or auto-generated)        | API contract; usually rendered from OpenAPI                     |
| Code comments                          | Why, not what                                                   |
| XML doc / JSDoc                        | Public abstractions and components                              |

---

## 2. Project README

The root `README.md` is the front door. Every newcomer reads it first.

### Required sections

```markdown
# <Project>

<1-paragraph description: what this is, who uses it>

## Tech Stack
- Backend: .NET 10 LTS · ...
- Frontend: React 18.3 + Vite + Metronic · ...
- Database: PostgreSQL 16+

## Local Development
### Prerequisites
- .NET 10 SDK
- Node 22 LTS
- Docker Desktop
- (optional) Visual Studio 2026 / Rider / VS Code

### Setup
1. Clone the repo
2. `docker compose up -d postgres redis`
3. `dotnet user-secrets set ...` (see below)
4. `dotnet ef database update --project ...`
5. `dotnet run --project src/<Project>.Api`
6. `cd <Project>.Client && npm install && npm run dev`

### Required user-secrets
- `Jwt:SigningKey`  — any 32+ char string
- `ConnectionStrings:Default` — points at local Postgres

## Project Structure
<link to docs/architecture/solution-overview.md>

## Conventions
See `docs/conventions/` (or wherever).

## Common Tasks
- Run tests: `dotnet test` / `npm test`
- Add a backend module: see `docs/conventions/08-templates/module-implementation-checklist.md`
- Update OpenAPI types: `npm run openapi:gen`

## Deployment
See `docs/runbooks/deployment.md`.

## License
<license>
```

### Rules

- **README is current.** Outdated setup instructions are the most common onboarding pain point.
- **Cross-references** to deeper docs, not duplication.
- **Update README in the same PR** as the change that breaks the existing instructions.

---

## 3. CLAUDE.md (AI Assistant Memory)

Every repo has a `CLAUDE.md` that briefs AI tools on the project. See `08-templates/claude-md-template.md` for the full template.

### Rules

- **Mandatory file at repo root.**
- **Points at the conventions library.** AI tools read `CLAUDE.md` and follow the rules there.
- **Lists project-specific overrides** (rarely; document any deviation explicitly).

---

## 4. Architecture Documentation

`docs/architecture/solution-overview.md`:

```markdown
# Solution Overview

## Bounded Contexts
<list with one-paragraph descriptions>

## Module Map
<table: module → owner → main aggregates>

## System Diagram
<diagram or link>

## Data Flow
<happy-path request walkthrough>

## Cross-Cutting Concerns
<auth, logging, audit, idempotency>

## External Dependencies
<list: DB, Redis, message broker, 3rd-party APIs>
```

### Rules

- **One pager per project.** Concise; not a textbook.
- **Diagrams in Mermaid** (renders in GitHub) or PNG / SVG checked into `docs/architecture/diagrams/`.
- **Updated when the architecture changes**, not after.

---

## 5. Architecture Decision Records (ADRs)

Each significant decision gets an ADR in `docs/decisions/`. See `08-templates/adr-template.md`.

```
docs/decisions/
├── adr-001-clean-architecture.md
├── adr-002-postgresql-snake-case.md
├── adr-003-rtk-query-over-tanstack-query.md
└── adr-004-jwt-with-refresh-tokens.md
```

### What's worth an ADR?

- Choice of a major library (RTK Query vs TanStack Query, MediatR vs minimal CQRS).
- Architectural pattern (Clean Architecture, modular monolith).
- Data store decisions (PostgreSQL, Redis, separate read DB).
- Auth strategy.
- Any deviation from the convention library.

### What's NOT worth an ADR?

- "We use Tailwind." (It's in the conventions.)
- "We use sealed classes." (It's in the conventions.)
- A single function's implementation choice.

### Rules

- **ADRs are immutable once accepted.** New ADR supersedes old; mark the old one **Superseded by ADR-NNN**.
- **Format**: context → decision → consequences. See template.
- **Number sequentially.** `adr-001`, `adr-002`, etc.

---

## 6. Runbooks

Operational procedures live in `docs/runbooks/`:

```
docs/runbooks/
├── deployment.md
├── rollback.md
├── database-backup-restore.md
├── incident-response.md
├── on-call.md
├── secret-rotation.md
└── certificate-renewal.md
```

### Runbook shape

```markdown
# <Procedure name>

## When to use
<trigger condition>

## Prerequisites
<access, tools, credentials>

## Steps
1. ...
2. ...

## Verification
<how to confirm success>

## Rollback
<if applicable>

## Escalation
<who to contact if stuck>
```

### Rules

- **Step-by-step.** Assume the reader is on-call at 3 AM and tired.
- **Tested annually** by performing the procedure in staging.
- **No links to chat threads** — they expire. Inline the relevant info.

---

## 7. Database Documentation

`docs/database/schema-overview.md`:

- Schemas (`app`, `auth`, `audit`, `outbox`).
- Aggregate-to-table mapping.
- Notable indexes and why.
- Migration timeline per release.

### Rules

- **Don't try to maintain a hand-written ERD.** It rots. Generate from the live schema with a tool (DBeaver, pgModeler).
- **Document business rules expressed at the DB level** (CHECK constraints, exclusion constraints) — they're easy to miss.

---

## 8. API Documentation

API documentation is **generated from the OpenAPI spec**, not hand-written.

- **Backend**: Swashbuckle generates `/swagger/v1/swagger.json` from XML doc comments + `Produces<T>` attributes.
- **Hosted Swagger UI** at `/swagger` in dev / staging.
- **Static export** for documentation sites: `swagger-ui-dist` or Redoc rendering the JSON.
- **Frontend types** generated from the same spec.

### Rules

- **Backend XML doc comments** on endpoint methods — they end up in Swagger UI.
- **`Produces<T>`** on every endpoint for accurate response schemas.
- **No separate API doc site.** OpenAPI is the source of truth.

---

## 9. Code Comments

### When to comment

- **The why is non-obvious.** Workaround for a library bug, regulatory rule, performance trade-off.
- **A subtle invariant.** "This must run before X because..."
- **A surprising design.** Explain the reasoning so future-you doesn't "fix" it.

### When NOT to comment

- **What the code does.** Well-named identifiers handle that.
- **References to ephemeral context** ("for the Smith account migration"). Use git history / commit messages.
- **Restating the function name.** `// Calculate total` above `decimal CalculateTotal()` — delete.

### Format

```csharp
// Customer email is stored lowercased to match RFC 5321 §2.4 case-insensitive
// local-part comparison policy. Display formatting is handled at the UI layer.
public Email Email { get; }
```

### Rules

- **Comments explain *why*.**
- **`// TODO(TICKET-ID): description`** for known follow-ups.
- **No commented-out code in commits.** Use git history.

---

## 10. XML Doc Comments (C#)

```csharp
/// <summary>
/// Issues an access token for an authenticated user.
/// </summary>
/// <param name="user">The authenticated user.</param>
/// <param name="roles">User's role names.</param>
/// <param name="permissions">Flattened permissions from the user's roles.</param>
/// <returns>An access token with its expiration timestamp.</returns>
public AccessToken GenerateAccessToken(
    User user,
    IReadOnlyCollection<string> roles,
    IReadOnlyCollection<string> permissions) { ... }
```

### Rules

- **All public types and members in `Domain/Abstractions/` and `Application/Abstractions/`** have XML doc.
- **Endpoint methods** have `/// <summary>` so Swagger renders them.
- **Internal types** are exempt unless they're tricky.

---

## 11. JSDoc (TypeScript)

```ts
/**
 * Formats a numeric amount as currency using the user's locale.
 *
 * @example
 * formatCurrency(1234.56, 'USD') // "$1,234.56"
 */
export function formatCurrency(amount: number, currency: string): string { ... }
```

### Rules

- **Public utility functions** in `src/lib/` have JSDoc.
- **Components in `src/components/ui/`** have a one-line JSDoc summary.
- **Module-internal helpers** are exempt.

---

## 12. Common Mistakes

| Mistake                                                       | Fix                                                              |
|---------------------------------------------------------------|------------------------------------------------------------------|
| README out of date with setup steps                           | Update in the same PR that broke the steps                       |
| ADRs skipped because "we'll remember why"                     | You won't                                                        |
| Documentation in chat / shared drive only                     | In the repo, in markdown                                          |
| Hand-written API docs that drift from the code                | Generated from OpenAPI                                            |
| `# TODO: figure this out later` without a ticket              | `// TODO(<TICKET>):` or delete                                   |
| ERD generated once and never updated                          | Generate on demand from the live schema                          |
| Comments restating what the code does                         | Delete; rename the variable instead                              |
| Runbooks tested only during real incidents                    | Test in staging quarterly                                        |
| Conventions not documented or pointed at                      | The conventions library is the source of truth                   |
