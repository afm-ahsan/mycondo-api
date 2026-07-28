---
name: database-migrations
description: How to add EF Core migrations correctly in MyCondo — schema-per-module placement, naming, and a real gotcha already hit once (analyzer-breaking namespace style in generated files).
---

# Database Migrations

## Commands

```powershell
dotnet ef migrations add Add_<Subject> `
  --project src/MyCondo.Infrastructure `
  --startup-project src/MyCondo.Api

dotnet ef database update `
  --project src/MyCondo.Infrastructure `
  --startup-project src/MyCondo.Api
```

## Schema placement

Schema-per-module is the approved strategy (ADR-004) — every new table's `IEntityTypeConfiguration<T>`
must call `.ToTable("<table>", schema: "<owning-module-schema>")` using the schema→module map in
`mycondo-api/docs/kickoff.md` (`tenancy`, `identity`, `property`, `residents`, `leasing`, `billing`,
`payments`, `expenses`, `vendors`, `payroll`, `complaints`, `maintenance`, `amenities`, `security`,
`notifications`, `documents`, `reporting`, `audit`). Never rely on a default schema — `MyCondoDbContext`
deliberately has no `HasDefaultSchema()` so every table's schema is explicit.

## Naming (snake_case everywhere)

Tables plural; PK `id` (uuid v7 via `IIdGenerator`); FK `<singular>_id`; indexes `ix_<table>_<cols>`;
uniques `ux_...`; FK constraints `fk_<table>_<ref>`; checks `ck_<table>_<rule>`; RLS policies
`rls_<table>_tenant_isolation`; views `vw_...`; materialized views `mv_...`; functions `fn_...`;
triggers `tr_<table>_<event>`. Composite indexes on tenant-scoped tables always lead with `tenant_id`.

## A gotcha already hit once — fix it if you hit it again

`Directory.Build.props` sets `EnforceCodeStyleInBuild=true` + `TreatWarningsAsErrors=true`, and
`.editorconfig` prefers file-scoped namespaces. `dotnet ef migrations add` scaffolds generated
`.cs`/`.Designer.cs` files with **block-scoped** namespaces by default, which trips `IDE0161` and
fails the build. After scaffolding a new migration, check:

```
grep -n "^namespace" src/MyCondo.Infrastructure/Persistence/Migrations/<new-file>*.cs
```

If any use `namespace X\n{` instead of `namespace X;`, convert them (or run `dotnet build` immediately
after scaffolding — it will fail loudly and tell you exactly which file).

## Every tenant-scoped table needs

`tenant_id uuid not null` from day one, even before the RLS policy for it is written (see
`postgresql-rls.md`) — retrofitting the column later is much more disruptive than including it up
front on every new table in scope for multi-tenancy.
