---
name: postgresql-rls
description: MyCondo's Row-Level Security and multi-tenancy approach — implemented, enforced, and how to extend it to new tenant-scoped tables. Use for any migration, DbContext, or tenant-context change.
---

# PostgreSQL RLS & Multi-Tenancy

## Current actual state (2026-07-28, Wave 1 Slice 2)

RLS is real, not aspirational, on 5 tables: `identity.users`, `roles`, `role_assignments`,
`refresh_tokens`, `role_permissions` — each has `ENABLE ROW LEVEL SECURITY`, `FORCE ROW LEVEL
SECURITY`, and a `rls_<table>_tenant_isolation` policy (migration `Enable_Tenant_Row_Level_Security`).
`FORCE` is mandatory here, not optional — there is only one Postgres role (`mycondo_app`) in this
setup, and it owns the tables it creates via migrations, so plain `ENABLE` alone would not restrict it.

**Not every tenant-related table has (or needs) `tenant_id`:** `identity.permissions` is global
reference data (same catalogue across all tenants) — correctly has no `tenant_id`, no RLS.
`tenancy.tenants` is the tenant root itself — also correctly has neither.

## How tenant context reaches the database

`TenantContextConnectionInterceptor` (`src/MyCondo.Infrastructure/Persistence/Interceptors/`) sets
`app.current_tenant_id` via `set_config` on **every** `ConnectionOpened`/`ConnectionOpenedAsync` —
reads and writes alike. This replaced an earlier approach that only set it in
`MyCondoDbContext.SaveChangesAsync` (writes only), which would have made reads silently return zero
rows once RLS was enabled. The interceptor **always** calls `set_config`, using `''` when there's no
current tenant — it never skips the call. Skipping would let a stale tenant value survive on a pooled
connection into an unrelated request (Npgsql connection pooling reuses physical connections across
`DbContext` instances; `ConnectionOpened` fires on every logical open regardless).

## Policy shape — copy this exactly for any new tenant-scoped table

```sql
ALTER TABLE <schema>.<table> ENABLE ROW LEVEL SECURITY;
ALTER TABLE <schema>.<table> FORCE ROW LEVEL SECURITY;
CREATE POLICY rls_<table>_tenant_isolation ON <schema>.<table>
  USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
  WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
```

**Do not write `tenant_id = current_setting('app.current_tenant_id')::uuid` without the `NULLIF`.**
The connection interceptor sets the GUC to `''` for anonymous requests, and `''::uuid` raises a
Postgres error rather than evaluating to NULL — a naive cast turns every anonymous RLS-protected query
into a 500 instead of a safe empty result. `NULLIF(..., '')` collapses both "never set" and
"explicitly cleared" to NULL first, so the comparison itself evaluates to NULL (no visible rows).

Include `WITH CHECK` explicitly (don't rely on `USING` alone covering writes) — it's what rejects an
`INSERT`/`UPDATE` for the wrong tenant with a clear RLS-violation error instead of silently succeeding.

## Adding RLS to a new tenant-scoped table

1. Confirm the table actually has `tenant_id` (add it in its own migration first if not — see
   `role_permissions`' `Add_TenantId_To_RolePermissions` migration for the pattern when retrofitting).
2. Add the table name to `TenantScopedTables` in a new migration modeled on
   `Enable_Tenant_Row_Level_Security` (or extend that array in a follow-up migration).
3. Add a cross-tenant test in `MyCondo.MultiTenancyTests` before/alongside enabling the policy — don't
   assume it works.

## Testing

`MyCondo.MultiTenancyTests` uses a real Testcontainers-backed Postgres (`MultiTenancyPostgresFixture`)
and constructs `MyCondoDbContext` directly with a settable `TestTenantContextAccessor` — no HTTP
involved, this project is specifically about DB-level isolation. Covers: cross-tenant read isolation,
`WITH CHECK` rejecting a wrong-tenant insert, and `pg_class` verification that `relrowsecurity`/
`relforcerowsecurity` are actually set on every expected table. **These need a Docker daemon and were
written/compile-verified but not executed** in the environment they were authored in — run them
wherever Docker is available before trusting them.
