---
name: postgresql-rls
description: MyCondo's Row-Level Security and multi-tenancy approach, including the current gap (no policies exist yet) and the sequencing risk when adding them. Use for any migration, DbContext, or tenant-context change.
---

# PostgreSQL RLS & Multi-Tenancy

## Current actual state (2026-07-28)

- Every tenant-scoped table has `tenant_id uuid not null` (already true for the `identity` schema
  tables).
- `MyCondoDbContext.SaveChangesAsync` runs `SELECT set_config('app.current_tenant_id', {tenantId}, false)`
  before saving, sourced from `ITenantContextAccessor.CurrentTenantId`.
- **No RLS policy exists in any migration.** No `ENABLE ROW LEVEL SECURITY`, no `FORCE ROW LEVEL
  SECURITY`, no `CREATE POLICY`. Do not assume tenant isolation is enforced at the database level yet
  — today it is enforced (partially) only by application-level query filtering, which is not the
  target architecture and must not be treated as sufficient.

## Target state

Schema-per-module is the approved DB strategy (ADR-004) — RLS applies per-table regardless of schema:

```sql
ALTER TABLE <schema>.<table> ENABLE ROW LEVEL SECURITY;
ALTER TABLE <schema>.<table> FORCE ROW LEVEL SECURITY;
CREATE POLICY rls_<table>_tenant_isolation ON <schema>.<table>
  USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
```

Naming convention: `rls_<table>_tenant_isolation`.

## The sequencing trap

`FORCE ROW LEVEL SECURITY` applies even to the table owner. If it's turned on for a table before
every **read** path reliably sets `app.current_tenant_id` (today the session variable is only
guaranteed set on `SaveChangesAsync`, i.e. writes), reads will silently return **zero rows** instead
of failing loudly — this looks like "the feature returns nothing" bugs, not permission errors. Before
enabling RLS on a table:

1. Confirm the tenant context is set at the start of every request pipeline (not just on save) —
   likely needs a middleware or a required call at `DbContext` construction/connection-open time, not
   only in `SaveChangesAsync`.
2. Write the cross-tenant-must-fail test in `MyCondo.MultiTenancyTests` **before** enabling the policy,
   so you have a red test that goes green, not an untested assumption.
3. Only then add the migration enabling RLS for that table.

## Testing

`MyCondo.MultiTenancyTests` exists as a project but currently only contains the placeholder test.
Every RLS policy added needs: (a) same-tenant read/write succeeds, (b) cross-tenant read/write fails,
(c) queries with no tenant context set return zero rows (not an error, not all tenants' data).
