# Module Implementation Checklist

Use this checklist when adding a **new business module** (e.g. `Customers`, `Quotations`, `Invoices`). It defines the order of work — skipping ahead causes rework.

> Replace `<Aggregate>` (PascalCase, singular: `Customer`), `<feature>` (kebab-case, plural: `customers`), and `<schema>` (`app` by default).

---

## Phase 1: Plan

- [ ] **Identify the aggregate root.** What's the unit of consistency? (`Customer`, `Quotation`, `Invoice`).
- [ ] **List value objects and child entities.** (`Email`, `Money`, `ContactPerson`).
- [ ] **List domain events** the aggregate raises (`<Aggregate>Created`, `<Aggregate>StateChanged`).
- [ ] **List use cases**: commands and queries.
- [ ] **Identify required permissions** (`<feature>.view`, `<feature>.manage`).
- [ ] **Sketch the URL space**: `/api/<feature>`, `/api/<feature>/{id}`, special verbs.
- [ ] **Open an issue** linking these decisions; PR description references it.

---

## Phase 2: Backend — Domain

`src/<Project>.Domain/<Aggregate>s/`

- [ ] `<Aggregate>Id.cs` — strongly-typed ID (`readonly record struct`).
- [ ] `<Aggregate>.cs` — aggregate root with private setters and methods.
- [ ] `<Aggregate>Status.cs` — state enum (smart enum if behavior).
- [ ] Value objects (`<ValueObject>.cs`) — private to the aggregate or in `Common/ValueObjects/`.
- [ ] `Events/<Aggregate>CreatedEvent.cs`, `Events/<Aggregate>UpdatedEvent.cs`.
- [ ] `Abstractions/I<Aggregate>Repository.cs`.
- [ ] Unit tests in `tests/<Project>.Domain.UnitTests/<Aggregate>s/`:
  - [ ] `Create_WithValidInputs_RaisesEvent`.
  - [ ] `Create_WithInvalid_<Field>_Throws` for each invariant.
  - [ ] One test per state transition method.

---

## Phase 3: Backend — Application

`src/<Project>.Application/<Aggregate>s/`

### DTOs

- [ ] `Dtos/<Aggregate>Dto.cs` — list/summary shape.
- [ ] `Dtos/<Aggregate>DetailDto.cs` — full detail with nested children.
- [ ] `<Aggregate>Mapping.cs` — manual `ToDto()` extension methods.

### Commands

For each write use case (`Create`, `Update`, `Deactivate`, `Delete`, ...):

- [ ] `Commands/<Verb><Aggregate>/<Verb><Aggregate>Command.cs`.
- [ ] `Commands/<Verb><Aggregate>/<Verb><Aggregate>CommandValidator.cs`.
- [ ] `Commands/<Verb><Aggregate>/<Verb><Aggregate>CommandHandler.cs`.
- [ ] Unit test in `tests/<Project>.Application.UnitTests/...`:
  - [ ] Happy path.
  - [ ] Validation failure.
  - [ ] Conflict / not-found cases.

### Queries

For each read use case (`GetById`, `Search`, `ListByX`):

- [ ] `Queries/<Verb><Aggregate>/<Verb><Aggregate>Query.cs`.
- [ ] `Queries/<Verb><Aggregate>/<Verb><Aggregate>QueryValidator.cs` (paging bounds).
- [ ] `Queries/<Verb><Aggregate>/<Verb><Aggregate>QueryHandler.cs`.

### Event Handlers (if needed)

- [ ] `EventHandlers/<Aggregate>CreatedEventHandler.cs` for side effects.

---

## Phase 4: Backend — Infrastructure

`src/<Project>.Infrastructure/Persistence/`

- [ ] `Configurations/<Aggregate>Configuration.cs` — EF Core entity configuration:
  - [ ] `ToTable("<aggregate>", schema: "<schema>")`.
  - [ ] `HasKey(...).HasName("pk_<aggregate>")`.
  - [ ] `HasConversion` for the strongly-typed ID.
  - [ ] `OwnsOne` / `OwnsMany` for value objects and children.
  - [ ] `IsConcurrencyToken()` on `Version`.
  - [ ] `HasQueryFilter(x => x.DeletedAtUtc == null)` for soft delete.
  - [ ] Explicit index names (`ix_<aggregate>_*`, `uix_<aggregate>_*`).
  - [ ] `Ignore(x => x.DomainEvents)`.
- [ ] `Repositories/<Aggregate>Repository.cs` — `I<Aggregate>Repository` impl.
- [ ] `ReadModels/<Aggregate>ReadRepository.cs` — Dapper for reads.
- [ ] Add `DbSet<<Aggregate>>` to `AppDbContext`.
- [ ] Register repos in `DependencyInjection.cs`.
- [ ] Migration:
  ```bash
  dotnet ef migrations add Add_<Aggregate>_Table \
    --project src/<Project>.Infrastructure \
    --startup-project src/<Project>.Api
  ```
  - [ ] Migration comment block at top describes intent.
  - [ ] `Up` and `Down` reviewed.
  - [ ] Apply locally and verify schema.
- [ ] Integration test in `tests/<Project>.Infrastructure.IntegrationTests/`:
  - [ ] Real Postgres via Testcontainers.
  - [ ] Insert + retrieve round-trip.
  - [ ] Constraints verified (unique, check, exclusion).

---

## Phase 5: Backend — Api

`src/<Project>.Api/`

- [ ] `Contracts/Requests/Create<Aggregate>Request.cs`, `Update<Aggregate>Request.cs`, `Search<Aggregate>sRequest.cs`.
- [ ] `Endpoints/<Aggregate>Endpoints.cs`:
  - [ ] `MapGroup("/api/<feature>").RequireAuthorization()`.
  - [ ] `MapPost("/", CreateAsync).RequireAuthorization("<feature>.manage")`.
  - [ ] `MapGet("/{id:guid}", GetByIdAsync).RequireAuthorization("<feature>.view")`.
  - [ ] `MapGet("/", SearchAsync).RequireAuthorization("<feature>.view")`.
  - [ ] `MapPut("/{id:guid}", UpdateAsync).RequireAuthorization("<feature>.manage")`.
  - [ ] `MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization("<feature>.manage")`.
  - [ ] `Produces<T>` and `ProducesProblem(...)` on every endpoint.
- [ ] Wire in `Program.cs`: `app.Map<Aggregate>Endpoints();`.
- [ ] Permissions added to `Permissions` constants and seeded into the DB.
- [ ] Integration test in `tests/<Project>.Api.IntegrationTests/`:
  - [ ] Authenticated POST returns 201.
  - [ ] Unauthenticated returns 401.
  - [ ] Forbidden role returns 403.
  - [ ] Validation error returns 400 with field map.
  - [ ] Duplicate returns 409.

---

## Phase 6: Frontend — Module

`src/modules/<feature>/`

### API

- [ ] `api/<feature>.types.ts` — request/response types (or generated from OpenAPI).
- [ ] `api/<feature>.api.ts` — RTK Query slice with:
  - [ ] `search<Aggregate>s` query.
  - [ ] `get<Aggregate>ById` query.
  - [ ] `create<Aggregate>` mutation.
  - [ ] `update<Aggregate>` mutation.
  - [ ] `delete<Aggregate>` mutation.
  - [ ] Tags: `{ type: '<Aggregate>', id }` + `{ type: '<Aggregate>', id: 'LIST' }`.
- [ ] Add `<Aggregate>` to `tagTypes` in `src/api/base-query.ts`.

### Schemas

- [ ] `schemas/<feature>.schema.ts` — Zod schema for create/edit form.
- [ ] Inferred type exported.

### Components

- [ ] `components/<Aggregate>Form.tsx` — RHF form (no data fetching inside).
- [ ] `components/<Aggregate>List.tsx` — table component.
- [ ] `components/<Aggregate>StatusBadge.tsx` (if status display needed).
- [ ] `components/*.test.tsx` for each component.

### Pages

- [ ] `pages/<Aggregate>sListPage.tsx`.
- [ ] `pages/<Aggregate>CreatePage.tsx`.
- [ ] `pages/<Aggregate>EditPage.tsx`.
- [ ] `pages/<Aggregate>DetailPage.tsx`.
- [ ] `pages/<Aggregate>sRouter.tsx` — module router with permission guards.

### Public surface

- [ ] `index.ts` — `export { default as <Aggregate>sRouter } from './pages/<Aggregate>sRouter'`.

### Routes

- [ ] Lazy-load module in `src/routes/app-routes.tsx`:
  ```tsx
  const <Aggregate>sRouter = lazy(() => import('@/modules/<feature>'));
  ```
- [ ] Add `<Route path="<feature>/*" element={<<Aggregate>sRouter />} />`.
- [ ] Add menu item in `src/config/menu.config.ts` (gated by permission).

---

## Phase 7: Tests (E2E)

`e2e/tests/<feature>.spec.ts`:

- [ ] Login as a user with `<feature>.manage` permission.
- [ ] Create an `<Aggregate>` via the UI; verify it appears in the list.
- [ ] Edit an `<Aggregate>`; verify changes persist.
- [ ] Delete an `<Aggregate>`; verify removed from list.
- [ ] Login as a user without `<feature>.manage`; verify "Add" button is hidden.

---

## Phase 8: Observability

- [ ] Handlers log structured info events on success.
- [ ] Permissions added to seed data and assigned to roles.
- [ ] Domain events published; integration events on the bus (if relevant).
- [ ] Cache keys defined in `CacheKeys` if caching is used.
- [ ] OpenAPI updated (Swashbuckle picks this up automatically; verify visually).

---

## Phase 9: Documentation

- [ ] README updated if setup steps changed.
- [ ] ADR opened if any non-obvious choice was made (e.g. dropping a column, choosing a 3rd-party API).
- [ ] If new permissions were added, document them in `docs/architecture/permissions.md` (if maintained).

---

## Phase 10: PR

- [ ] All checklists above complete.
- [ ] CI green: backend tests, frontend tests, E2E tests, CodeQL.
- [ ] PR description follows the template.
- [ ] One reviewer approves.
- [ ] Squash-merge.
- [ ] Branch deleted.

---

## Quick Reference — File Count Per Module

| Layer            | Approx. # of files |
|------------------|--------------------|
| Domain           | ~6 (entity, ID, status, 2-3 events, repository interface) |
| Application      | ~15-20 (5 commands × 3 files + 3 queries × 3 files + DTOs + mapping) |
| Infrastructure   | ~3 (config, repo, read repo) + 1 migration |
| Api              | ~3 (endpoint group, request DTOs) |
| Frontend module  | ~12 (api, schema, 3-4 components, 4 pages, router, types) |
| Tests            | matches each layer 1:1 |

A new module is ~50-70 files. That's normal — the predictability is the value.
